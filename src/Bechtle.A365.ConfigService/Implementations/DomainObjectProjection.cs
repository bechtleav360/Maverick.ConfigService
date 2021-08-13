using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.Services.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Component that receives
    /// </summary>
    public class DomainObjectProjection : EventSubscriptionBase
    {
        private readonly IOptions<EventStoreConnectionConfiguration> _configuration;
        private readonly DomainEventProjectionCheck _healthCheck;
        private readonly ILogger<DomainObjectProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectProjection" />
        /// </summary>
        /// <param name="eventStore">EventStore that handles the underlying subscription</param>
        /// <param name="objectStore">store for the projected DomainObjects</param>
        /// <param name="serviceProvider">serviceProvider used to retrieve components for compiling configurations</param>
        /// <param name="configuration">options used to configure the subscription</param>
        /// <param name="healthCheck">associated Health-Check that reports the current status of this component</param>
        /// <param name="logger">logger to write information to</param>
        public DomainObjectProjection(
            IEventStore eventStore,
            IDomainObjectStore objectStore,
            IServiceProvider serviceProvider,
            IOptions<EventStoreConnectionConfiguration> configuration,
            DomainEventProjectionCheck healthCheck,
            ILogger<DomainObjectProjection> logger) : base(eventStore)
        {
            _objectStore = objectStore;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _healthCheck = healthCheck;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async void ConfigureStreamSubscription(IStreamSubscriptionBuilder subscriptionBuilder)
        {
            long lastProjectedEvent = -1;
            try
            {
                IResult<long> result = await _objectStore.GetProjectedVersion();
                if (result.IsError)
                {
                    _logger.LogWarning("unable to tell which event was last projected, starting from scratch");
                }
                else
                {
                    _logger.LogInformation("starting DomainEvent-Projection at event {LastProjectedEvent}", lastProjectedEvent);
                    lastProjectedEvent = result.Data;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "error while checking which event was last projected, starting from scratch");
            }

            subscriptionBuilder.ToStream(_configuration.Value.Stream);

            // having projected event-0 is a valid possibility
            if (lastProjectedEvent < 0)
            {
                subscriptionBuilder.FromStart();
            }
            else
            {
                // we're losing half the amount of events we could be projecting
                // but the ConfigService isn't meant to handle either
                // 18.446.744.073.709.551.615 or 9.223.372.036.854.775.807 events, so cutting off half our range seems fine
                subscriptionBuilder.FromEvent((ulong) lastProjectedEvent);
            }

            _healthCheck.SetReady();
        }

        /// <inheritdoc />
        protected override async Task OnDomainEventReceived(StreamedEventHeader eventHeader, IDomainEvent domainEvent)
        {
            _healthCheck.SetReady();

            try
            {
                _logger.LogInformation(
                    "projecting domainEvent #{EventNumber} with id {EventId} of type {EventType}",
                    eventHeader.EventNumber,
                    eventHeader.EventId,
                    eventHeader.EventType);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Task task = domainEvent switch
                {
                    IDomainEvent<ConfigurationBuilt> e => HandleConfigurationBuilt(eventHeader, e),
                    IDomainEvent<DefaultEnvironmentCreated> e => HandleDefaultEnvironmentCreated(eventHeader, e),
                    IDomainEvent<EnvironmentCreated> e => HandleEnvironmentCreated(eventHeader, e),
                    IDomainEvent<EnvironmentDeleted> e => HandleEnvironmentDeleted(eventHeader, e),
                    IDomainEvent<EnvironmentLayersModified> e => HandleEnvironmentLayersModified(eventHeader, e),
                    IDomainEvent<EnvironmentLayerCreated> e => HandleEnvironmentLayerCreated(eventHeader, e),
                    IDomainEvent<EnvironmentLayerDeleted> e => HandleEnvironmentLayerDeleted(eventHeader, e),
                    IDomainEvent<EnvironmentLayerCopied> e => HandleEnvironmentLayerCopied(eventHeader, e),
                    IDomainEvent<EnvironmentLayerTagsChanged> e => HandleEnvironmentLayerTagsChanged(eventHeader, e),
                    IDomainEvent<EnvironmentLayerKeysImported> e => HandleEnvironmentLayerKeysImported(eventHeader, e),
                    IDomainEvent<EnvironmentLayerKeysModified> e => HandleEnvironmentLayerKeysModified(eventHeader, e),
                    IDomainEvent<StructureCreated> e => HandleStructureCreated(eventHeader, e),
                    IDomainEvent<StructureDeleted> e => HandleStructureDeleted(eventHeader, e),
                    IDomainEvent<StructureVariablesModified> e => HandleStructureVariablesModified(eventHeader, e),
                    _ => null
                };

                if (task is null)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "domainEvent #{EventNumber} of type {DomainEvent} was not projected - missing event-handler",
                        eventHeader.EventNumber,
                        eventHeader.EventType);
                    return;
                }

                await task;

                await _objectStore.SetProjectedVersion(
                    eventHeader.EventId.ToString("D"),
                    (long) eventHeader.EventNumber,
                    eventHeader.EventType);

                stopwatch.Stop();

                KnownMetrics.ProjectionTime
                            .WithLabels(eventHeader.EventType)
                            .Observe(stopwatch.Elapsed.TotalSeconds);

                KnownMetrics.EventsProjected
                            .WithLabels(eventHeader.EventType)
                            .Inc();
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "error while projecting domainEvent {DomainEventId}#{DomainEventNumber} of type {DomainEventType}",
                    eventHeader?.EventId,
                    eventHeader?.EventNumber,
                    eventHeader?.EventType);
                throw;
            }
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionDropped(string streamId, string streamName, Exception exception)
        {
            _healthCheck.SetReady(false);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionOpened()
        {
            _healthCheck.SetReady();
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Generate the autocomplete-paths for this layer
        /// </summary>
        /// <param name="keys">map of Key => Object</param>
        /// <returns></returns>
        private List<EnvironmentLayerKeyPath> GenerateKeyPaths(IDictionary<string, EnvironmentLayerKey> keys)
        {
            var roots = new List<EnvironmentLayerKeyPath>();

            foreach ((string key, EnvironmentLayerKey _) in keys.OrderBy(k => k.Key))
            {
                string[] parts = key.Split('/');

                string rootPart = parts.First();
                EnvironmentLayerKeyPath root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new EnvironmentLayerKeyPath(rootPart);
                    roots.Add(root);
                }

                EnvironmentLayerKeyPath current = root;

                foreach (string part in parts.Skip(1))
                {
                    EnvironmentLayerKeyPath next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

                    if (next is null)
                    {
                        next = new EnvironmentLayerKeyPath(part, current);
                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            return roots;
        }

        private async Task HandleConfigurationBuilt(StreamedEventHeader eventHeader, IDomainEvent<ConfigurationBuilt> domainEvent)
        {
            var config = new PreparedConfiguration(domainEvent.Payload.Identifier)
            {
                ConfigurationVersion = (long) domainEvent.Timestamp
                                                         .Subtract(DateTime.UnixEpoch)
                                                         .TotalSeconds,
                CurrentVersion = (long) eventHeader.EventNumber,
                ValidFrom = domainEvent.Payload.ValidFrom,
                ValidTo = domainEvent.Payload.ValidTo,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            using IServiceScope scope = _serviceProvider.CreateScope();

            var compiler = scope.ServiceProvider.GetRequiredService<IConfigurationCompiler>();
            var parser = scope.ServiceProvider.GetRequiredService<IConfigurationParser>();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            _logger?.LogDebug($"version used during compilation: {config.CurrentVersion}");

            // gather data to compile config with
            IResult<ConfigEnvironment> envResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(config.Id.Environment);
            if (envResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load environment to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    config.Id,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            IResult<ConfigStructure> structResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(config.Id.Structure);
            if (structResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load structure to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    config.Id,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            ConfigEnvironment environment = envResult.Data;
            ConfigStructure structure = structResult.Data;

            try
            {
                // compile the actual config
                CompilationResult compilationResult = compiler.Compile(
                    new EnvironmentCompilationInfo
                    {
                        Name = $"{config.Id.Environment.Category}/{config.Id.Environment.Name}",
                        Keys = environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)
                    },
                    new StructureCompilationInfo
                    {
                        Name = $"{config.Id.Structure.Name}/{config.Id.Structure.Version}",
                        Keys = structure.Keys,
                        Variables = structure.Variables
                    },
                    parser);

                // store result in DomainObject
                config.Keys = compilationResult.CompiledConfiguration;
                config.Json = translator.ToJson(config.Keys).ToString();
                config.UsedKeys = compilationResult.GetUsedKeys().ToList();
                config.CurrentVersion = (long) eventHeader.EventNumber;

                var tracerStack = new Stack<TraceResult>(compilationResult.CompilationTrace);
                while (tracerStack.TryPop(out TraceResult result))
                {
                    // most if not all traces will be returned as KeyTraceResult
                    if (!(result is KeyTraceResult keyResult))
                    {
                        continue;
                    }

                    if (keyResult.Errors.Any())
                    {
                        if (!config.Errors.TryGetValue(keyResult.Key, out List<string> errorList))
                        {
                            errorList = new List<string>();
                            config.Errors[keyResult.Key] = errorList;
                        }

                        errorList.AddRange(keyResult.Errors);
                    }

                    if (keyResult.Warnings.Any())
                    {
                        if (!config.Warnings.TryGetValue(keyResult.Key, out List<string> warningList))
                        {
                            warningList = new List<string>();
                            config.Errors[keyResult.Key] = warningList;
                        }

                        warningList.AddRange(keyResult.Warnings);
                    }
                }

                await _objectStore.Store<PreparedConfiguration, ConfigurationIdentifier>(config);

                // update metadata
                IResult<IDictionary<string, string>>
                    metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(config.Id);

                IDictionary<string, string> metadata;
                if (metadataResult.IsError)
                {
                    _logger.LogWarning("unable to update metadata for Configuration {ConfigurationIdentifier}, creating new one", config.Id);
                    metadata = new Dictionary<string, string>();
                }
                else
                {
                    metadata = metadataResult.Data;
                }

                metadata["used_layers"] = JsonConvert.SerializeObject(environment.Layers);
                metadata["stale"] = JsonConvert.SerializeObject(false);

                await _objectStore.StoreMetadata<PreparedConfiguration, ConfigurationIdentifier>(config, metadata);
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "failed to compile configuration, see exception for more details");
            }
        }

        private async Task HandleDefaultEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<DefaultEnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long) eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long) eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentDeleted(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentDeleted> domainEvent)
        {
            await _objectStore.Remove<ConfigEnvironment, EnvironmentIdentifier>(domainEvent.Payload.Identifier);
        }

        private async Task HandleEnvironmentLayerCopied(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerCopied> domainEvent)
        {
            IResult<EnvironmentLayer> sourceEnvironmentResult =
                await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.SourceIdentifier);

            if (sourceEnvironmentResult.IsError)
            {
                _logger.LogWarning(
                    "event received to copy layer, but source-layer wasn't found in configured store: {ErrorCode} {Message}",
                    sourceEnvironmentResult.Code,
                    sourceEnvironmentResult.Message);
                return;
            }

            EnvironmentLayer source = sourceEnvironmentResult.Data;
            var newLayer = new EnvironmentLayer(domainEvent.Payload.TargetIdentifier)
            {
                Json = source.Json,
                Keys = source.Keys,
                CurrentVersion = (long) eventHeader.EventNumber,
                KeyPaths = source.KeyPaths,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(newLayer);
        }

        private async Task HandleEnvironmentLayerCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerCreated> domainEvent)
        {
            var layer = new EnvironmentLayer(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long) eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);
        }

        private async Task HandleEnvironmentLayerDeleted(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerDeleted> domainEvent)
        {
            // remove this layers keys from all assigned environments
            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.Data;
            List<ConfigKeyAction> impliedActions = layer.Keys
                                                        .Select(k => ConfigKeyAction.Delete(k.Key))
                                                        .ToList();

            layer.Keys.Clear();
            layer.KeyPaths.Clear();
            layer.Json = "{}";
            layer.CurrentVersion = (long) eventHeader.EventNumber;

            using IServiceScope scope = _serviceProvider.CreateScope();

            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            await OnLayerKeysChanged(layer, impliedActions, translator);

            await _objectStore.Remove<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);
        }

        private async Task HandleEnvironmentLayerKeysImported(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerKeysImported> domainEvent)
        {
            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.Data;

            // set the 'Version' property of all changed Keys to the current unix-timestamp for later use
            var keyVersion = (long) DateTime.UtcNow
                                            .Subtract(DateTime.UnixEpoch)
                                            .TotalSeconds;

            // clear all currently-stored keys, because the import always overwrites whatever is there
            layer.Keys.Clear();
            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(a => a.Type == ConfigKeyActionType.Set))
            {
                // if we can find the new key with a different capitalization,
                // we remove the old and add the new one
                if (layer.Keys
                         .Select(k => k.Key)
                         .FirstOrDefault(k => k.Equals(change.Key, StringComparison.OrdinalIgnoreCase))
                        is { } existingKey)
                {
                    layer.Keys.Remove(existingKey);
                }

                layer.Keys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            layer.CurrentVersion = (long) eventHeader.EventNumber;
            layer.ChangedAt = domainEvent.Timestamp.ToUniversalTime();

            using IServiceScope scope = _serviceProvider.CreateScope();

            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            await OnLayerKeysChanged(layer, domainEvent.Payload.ModifiedKeys, translator);
        }

        private async Task HandleEnvironmentLayerKeysModified(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerKeysModified> domainEvent)
        {
            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.Data;

            foreach (ConfigKeyAction deletion in domainEvent.Payload
                                                            .ModifiedKeys
                                                            .Where(action => action.Type == ConfigKeyActionType.Delete))
            {
                if (layer.Keys.ContainsKey(deletion.Key))
                {
                    layer.Keys.Remove(deletion.Key);
                }
            }

            // set the 'Version' property of all changed Keys to the current unix-timestamp for later use
            var keyVersion = (long) DateTime.UtcNow
                                            .Subtract(DateTime.UnixEpoch)
                                            .TotalSeconds;

            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(a => a.Type == ConfigKeyActionType.Set))
            {
                // if we can find the new key with a different capitalization,
                // we remove the old and add the new one
                if (layer.Keys
                         .Select(k => k.Key)
                         .FirstOrDefault(k => k.Equals(change.Key, StringComparison.OrdinalIgnoreCase))
                        is { } existingKey)
                {
                    layer.Keys.Remove(existingKey);
                }

                layer.Keys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            layer.CurrentVersion = (long) eventHeader.EventNumber;
            layer.ChangedAt = domainEvent.Timestamp.ToUniversalTime();

            using IServiceScope scope = _serviceProvider.CreateScope();

            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            await OnLayerKeysChanged(layer, domainEvent.Payload.ModifiedKeys, translator);
        }

        private async Task HandleEnvironmentLayersModified(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayersModified> domainEvent)
        {
            IResult<ConfigEnvironment> envResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(domainEvent.Payload.Identifier);
            if (envResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify assigned layers, but environment wasn't found in configured store: {ErrorCode} {Message}",
                    envResult.Code,
                    envResult.Message);
                return;
            }

            ConfigEnvironment environment = envResult.Data;

            environment.Layers = domainEvent.Payload.Layers;

            IResult<Dictionary<string, EnvironmentLayerKey>> envDataResult = await ResolveEnvironmentKeys(environment);
            if (envDataResult.IsError)
            {
                _logger.LogWarning(
                    "unable to resolve complete list of keys for environment {Identifier}: {ErrorCode} {Message}",
                    environment.Id,
                    envDataResult.Code,
                    envDataResult.Message);
                return;
            }

            using IServiceScope scope = _serviceProvider.CreateScope();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            environment.Keys = envDataResult.Data;
            environment.Json = translator.ToJson(environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)).ToString();
            environment.KeyPaths = GenerateKeyPaths(environment.Keys);
            environment.CurrentVersion = (long) eventHeader.EventNumber;
            environment.ChangedAt = domainEvent.Timestamp.ToUniversalTime();

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentLayerTagsChanged(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerTagsChanged> domainEvent)
        {
            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.Data;

            foreach (string tag in domainEvent.Payload.AddedTags.Where(tag => !layer.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                layer.Tags.Add(tag);
            }

            foreach (string tag in domainEvent.Payload.RemovedTags)
            {
                string existingTag = layer.Tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (existingTag is null)
                    continue;
                layer.Tags.Remove(existingTag);
            }

            layer.CurrentVersion = (long) eventHeader.EventNumber;
            layer.ChangedAt = domainEvent.Timestamp.ToUniversalTime();

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);
        }

        private async Task HandleStructureCreated(StreamedEventHeader eventHeader, IDomainEvent<StructureCreated> domainEvent)
        {
            var structure = new ConfigStructure(domainEvent.Payload.Identifier)
            {
                Keys = domainEvent.Payload.Keys,
                Variables = domainEvent.Payload.Variables,
                CurrentVersion = (long) eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CreatedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(structure);
        }

        private async Task HandleStructureDeleted(StreamedEventHeader eventHeader, IDomainEvent<StructureDeleted> domainEvent)
        {
            await _objectStore.Remove<ConfigStructure, StructureIdentifier>(domainEvent.Payload.Identifier);
        }

        private async Task HandleStructureVariablesModified(StreamedEventHeader eventHeader, IDomainEvent<StructureVariablesModified> domainEvent)
        {
            IResult<ConfigStructure> structureResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(domainEvent.Payload.Identifier);

            if (structureResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify structure-vars, but structure wasn't found in configured store: {ErrorCode} {Message}",
                    structureResult.Code,
                    structureResult.Message);
                return;
            }

            ConfigStructure structure = structureResult.Data;

            foreach (ConfigKeyAction deletion in domainEvent.Payload
                                                            .ModifiedKeys
                                                            .Where(action => action.Type == ConfigKeyActionType.Delete))
            {
                if (structure.Variables.ContainsKey(deletion.Key))
                {
                    structure.Variables.Remove(deletion.Key);
                }
            }

            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(action => action.Type == ConfigKeyActionType.Set))
            {
                structure.Variables[change.Key] = change.Value;
            }

            structure.CurrentVersion = (long) eventHeader.EventNumber;
            structure.ChangedAt = domainEvent.Timestamp.ToUniversalTime();

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(structure);
        }

        private async Task OnLayerKeysChanged(EnvironmentLayer layer, ICollection<ConfigKeyAction> modifiedKeys, IJsonTranslator translator)
        {
            layer.Json = translator.ToJson(layer.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Key))
                                   .ToString();
            layer.KeyPaths = GenerateKeyPaths(layer.Keys);

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);

            IResult<Page<EnvironmentIdentifier>> envIdResult = await _objectStore.ListAll<ConfigEnvironment, EnvironmentIdentifier>(QueryRange.All);
            if (envIdResult.IsError)
            {
                return;
            }

            foreach (EnvironmentIdentifier envId in envIdResult.Data.Items)
            {
                IResult<ConfigEnvironment> environmentResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(envId);
                if (environmentResult.IsError)
                {
                    return;
                }

                ConfigEnvironment environment = environmentResult.Data;
                if (!environment.Layers.Contains(layer.Id))
                {
                    continue;
                }

                IResult<Dictionary<string, EnvironmentLayerKey>> environmentDataResult = await ResolveEnvironmentKeys(environment);
                if (environmentDataResult.IsError)
                {
                    return;
                }

                environment.Keys = environmentDataResult.Data;
                environment.Json = translator.ToJson(environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)).ToString();
                environment.KeyPaths = GenerateKeyPaths(environment.Keys);
                environment.CurrentVersion = layer.CurrentVersion;

                await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
            }

            await UpdateConfigurationStaleStatus(layer, modifiedKeys);

            Result.Success();
        }

        private async Task<IResult<Dictionary<string, EnvironmentLayerKey>>> ResolveEnvironmentKeys(ConfigEnvironment environment)
        {
            var environmentData = new Dictionary<string, EnvironmentLayerKey>();
            foreach (LayerIdentifier layerId in environment.Layers)
            {
                IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(layerId);
                if (layerResult.IsError)
                {
                    return Result.Error<Dictionary<string, EnvironmentLayerKey>>(layerResult.Message, layerResult.Code);
                }

                EnvironmentLayer layer = layerResult.Data;
                foreach ((string _, EnvironmentLayerKey key) in layer.Keys)
                {
                    environmentData[key.Key] = key;
                }
            }

            return Result.Success(environmentData);
        }

        // @TODO: un-/assignment of Layers in Environment don't currently mark a Configuration as Stale
        private async Task UpdateConfigurationStaleStatus(EnvironmentLayer layer, ICollection<ConfigKeyAction> modifiedKeys)
        {
            IResult<Page<ConfigurationIdentifier>> configIdResult = await _objectStore.ListAll<PreparedConfiguration, ConfigurationIdentifier>(QueryRange.All);

            if (configIdResult.IsError)
            {
                _logger.LogWarning("unable to load configs to update their stale-property");
                return;
            }

            foreach (ConfigurationIdentifier configId in configIdResult.Data.Items)
            {
                IResult<IDictionary<string, string>> metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(configId);
                if (metadataResult.IsError)
                {
                    _logger.LogWarning("unable to load metadata for config with id {Identifier} to update its stale-property", configId);
                    continue;
                }

                IDictionary<string, string> metadata = metadataResult.Data;

                // no key? assume it's not stale
                bool isStale = metadata.ContainsKey("stale") && JsonConvert.DeserializeObject<bool>(metadata["stale"]);

                if (isStale)
                {
                    _logger.LogDebug("config with id {Identifier} is already marked as stale - skipping re-calculation of staleness", configId);
                    continue;
                }

                List<LayerIdentifier> usedLayers = metadata.ContainsKey("used_layers")
                                                       ? JsonConvert.DeserializeObject<List<LayerIdentifier>>(metadata["used_layers"])
                                                       : null;

                if (usedLayers is null)
                {
                    _logger.LogWarning(
                        "unable to retrieve list of layers used to build config with id {Identifier} - unable to calculate staleness",
                        configId);
                    continue;
                }

                if (!usedLayers.Contains(layer.Id))
                {
                    _logger.LogDebug(
                        "config with id {ConfigId} did not use {LayerId} - this change will not make it stale",
                        configId,
                        layer.Id);
                    continue;
                }

                IResult<PreparedConfiguration> configResult = await _objectStore.Load<PreparedConfiguration, ConfigurationIdentifier>(configId);
                if (configResult.IsError)
                {
                    _logger.LogWarning("unable to load config with id {Identifier} to update its stale-property", configId);
                    continue;
                }

                PreparedConfiguration config = configResult.Data;

                List<string> usedKeys = config.UsedKeys;
                List<string> changedKeys = modifiedKeys.Select(k => k.Key).ToList();

                List<string> changedUsedKeys = changedKeys.Where(k => usedKeys.Contains(k, StringComparer.OrdinalIgnoreCase))
                                                          .ToList();
                if (changedUsedKeys.Any())
                {
                    _logger.LogDebug(
                        "config with id {ConfigId} used these keys which were now modified - marking as stale: {ChangedKeys}",
                        configId,
                        changedUsedKeys);

                    metadata["stale"] = JsonConvert.SerializeObject(true);
                    await _objectStore.StoreMetadata<PreparedConfiguration, ConfigurationIdentifier>(config, metadata);
                }
            }
        }
    }
}
