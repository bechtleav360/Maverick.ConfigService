using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
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
        private readonly ProjectionCacheCompatibleCheck _cacheHealthCheck;
        private readonly ProjectionStatusCheck _projectionStatus;
        private readonly IOptions<EventStoreConnectionConfiguration> _configuration;
        private readonly ILogger<DomainObjectProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly DomainEventProjectionCheck _projectionHealthCheck;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectProjection" />
        /// </summary>
        /// <param name="eventStore">EventStore that handles the underlying subscription</param>
        /// <param name="objectStore">store for the projected DomainObjects</param>
        /// <param name="serviceProvider">serviceProvider used to retrieve components for compiling configurations</param>
        /// <param name="configuration">options used to configure the subscription</param>
        /// <param name="projectionHealthCheck">associated Health-Check that reports the current status of this component</param>
        /// <param name="cacheHealthCheck">
        ///     health-check associated with <see cref="ProjectionCacheCleanupService" />. This Service will wait until the health-check is ready
        /// </param>
        /// <param name="projectionStatus">associated Status-Check that reports the current status of this component</param>
        /// <param name="logger">logger to write information to</param>
        public DomainObjectProjection(
            IEventStore eventStore,
            IDomainObjectStore objectStore,
            IServiceProvider serviceProvider,
            IOptions<EventStoreConnectionConfiguration> configuration,
            DomainEventProjectionCheck projectionHealthCheck,
            ProjectionCacheCompatibleCheck cacheHealthCheck,
            ProjectionStatusCheck projectionStatus,
            ILogger<DomainObjectProjection> logger) : base(eventStore)
        {
            _objectStore = objectStore;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _projectionHealthCheck = projectionHealthCheck;
            _cacheHealthCheck = cacheHealthCheck;
            _projectionStatus = projectionStatus;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override void ConfigureStreamSubscription(IStreamSubscriptionBuilder subscriptionBuilder)
        {
            long lastProjectedEvent = -1;
            try
            {
                IResult<long> result = _objectStore.GetProjectedVersion().Result;
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
                subscriptionBuilder.FromEvent((ulong)lastProjectedEvent);
            }

            _projectionHealthCheck.SetReady();
        }

        /// <summary>
        ///     Configure and start this Subscription, after <see cref="_cacheHealthCheck" /> is ready
        /// </summary>
        /// <param name="stoppingToken">token to stop this subscription with</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // stop normal subscription-flow until cache is killed / ready
            _logger.LogInformation("delaying subscription until the caches compatibility is checked");

            while (!_cacheHealthCheck.IsReady)
            {
                _logger.LogTrace(
                    "waiting for cache-compatibility-check to be ready: '{CompatibilityChecked}'",
                    _cacheHealthCheck.IsReady);
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }

            // back to our normal flow
            await base.ExecuteAsync(stoppingToken);
        }

        /// <inheritdoc />
        protected override async Task OnDomainEventReceived(
            StreamedEventHeader? eventHeader,
            IDomainEvent? domainEvent)
        {
            _projectionHealthCheck.SetReady();

            if (eventHeader is null)
            {
                _logger.LogWarning("unable to project domainEvent, no header provided - likely serialization problem");
                return;
            }

            if (domainEvent is null)
            {
                _logger.LogWarning(
                    "unable to project domainEvent #{EventNumber} with id {EventId} of type {EventType}, "
                    + "no body provided - likely serialization problem",
                    eventHeader.EventNumber,
                    eventHeader.EventId,
                    eventHeader.EventType);
                return;
            }

            try
            {
                _logger.LogInformation(
                    "projecting domainEvent #{EventNumber} with id {EventId} of type {EventType}",
                    eventHeader.EventNumber,
                    eventHeader.EventId,
                    eventHeader.EventType);

                _projectionStatus.SetCurrentlyProjecting(eventHeader);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Task? task = domainEvent switch
                {
                    IDomainEvent<ConfigurationBuilt> e => HandleConfigurationBuilt(eventHeader, e),
                    IDomainEvent<DefaultEnvironmentCreated> e => HandleDefaultEnvironmentCreated(eventHeader, e),
                    IDomainEvent<EnvironmentCreated> e => HandleEnvironmentCreated(eventHeader, e),
                    IDomainEvent<EnvironmentDeleted> e => HandleEnvironmentDeleted(e),
                    IDomainEvent<EnvironmentLayersModified> e => HandleEnvironmentLayersModified(eventHeader, e),
                    IDomainEvent<EnvironmentLayerCreated> e => HandleEnvironmentLayerCreated(eventHeader, e),
                    IDomainEvent<EnvironmentLayerDeleted> e => HandleEnvironmentLayerDeleted(eventHeader, e),
                    IDomainEvent<EnvironmentLayerCopied> e => HandleEnvironmentLayerCopied(eventHeader, e),
                    IDomainEvent<EnvironmentLayerTagsChanged> e => HandleEnvironmentLayerTagsChanged(eventHeader, e),
                    IDomainEvent<EnvironmentLayerKeysImported> e => HandleEnvironmentLayerKeysImported(eventHeader, e),
                    IDomainEvent<EnvironmentLayerKeysModified> e => HandleEnvironmentLayerKeysModified(eventHeader, e),
                    IDomainEvent<StructureCreated> e => HandleStructureCreated(eventHeader, e),
                    IDomainEvent<StructureDeleted> e => HandleStructureDeleted(e),
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
                    (long)eventHeader.EventNumber,
                    eventHeader.EventType);

                stopwatch.Stop();

                _projectionStatus.SetDoneProjecting(eventHeader);

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
                    eventHeader.EventId,
                    eventHeader.EventNumber,
                    eventHeader.EventType);
                _projectionStatus.SetErrorWhileProjecting(eventHeader, e);
                throw;
            }
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionDropped(string streamId, string streamName, Exception exception)
        {
            _projectionHealthCheck.SetReady(false);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionOpened()
        {
            _projectionHealthCheck.SetReady();
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
                EnvironmentLayerKeyPath? root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new EnvironmentLayerKeyPath(rootPart);
                    roots.Add(root);
                }

                EnvironmentLayerKeyPath current = root;

                foreach (string part in parts.Skip(1))
                {
                    EnvironmentLayerKeyPath? next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

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
            using IServiceScope scope = _serviceProvider.CreateScope();

            var compiler = scope.ServiceProvider.GetRequiredService<IConfigurationCompiler>();
            var parser = scope.ServiceProvider.GetRequiredService<IConfigurationParser>();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            _logger.LogDebug($"version used during compilation: {eventHeader.EventNumber}");

            ConfigurationIdentifier configId = domainEvent.Payload.Identifier;

            // gather data to compile config with
            IResult<ConfigEnvironment> envResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(configId.Environment);

            if (envResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load environment to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    configId,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            IResult<ConfigStructure> structResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(configId.Structure);

            if (structResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load structure to compile configuration {ConfigIdentifier}: {Code} {Message}",
                    configId,
                    envResult.Code,
                    envResult.Message);
                return;
            }

            ConfigEnvironment environment = envResult.CheckedData;
            ConfigStructure structure = structResult.CheckedData;

            try
            {
                // compile the actual config
                CompilationResult compilationResult = compiler.Compile(
                    new EnvironmentCompilationInfo
                    {
                        Name = $"{configId.Environment.Category}/{configId.Environment.Name}",
                        Keys = environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)
                    },
                    new StructureCompilationInfo
                    {
                        Name = $"{configId.Structure.Name}/{configId.Structure.Version}",
                        Keys = structure.Keys,
                        Variables = structure.Variables
                    },
                    parser);

                // store result in DomainObject
                var config = new PreparedConfiguration(configId)
                {
                    ConfigurationVersion = (long)domainEvent.Timestamp
                                                            .Subtract(DateTime.UnixEpoch)
                                                            .TotalSeconds,
                    CurrentVersion = (long)eventHeader.EventNumber,
                    ValidFrom = domainEvent.Payload.ValidFrom,
                    ValidTo = domainEvent.Payload.ValidTo,
                    CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                    ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                    Keys = compilationResult.CompiledConfiguration.ToDictionary(),
                    Json = translator.ToJson(compilationResult.CompiledConfiguration).ToString(),
                    UsedKeys = compilationResult.GetUsedKeys().ToList()
                };

                Stack<TraceResult> tracerStack = new(compilationResult.CompilationTrace);
                while (tracerStack.TryPop(out TraceResult? result))
                {
                    // most if not all traces will be returned as KeyTraceResult
                    if (result is not KeyTraceResult keyResult)
                    {
                        continue;
                    }

                    if (keyResult.Errors.Any())
                    {
                        if (keyResult.Key is null)
                        {
                            continue;
                        }

                        if (!config.Errors.TryGetValue(keyResult.Key, out List<string>? errorList))
                        {
                            errorList = new List<string>();
                            config.Errors[keyResult.Key] = errorList;
                        }

                        errorList.AddRange(keyResult.Errors);
                    }

                    if (keyResult.Warnings.Any())
                    {
                        if (keyResult.Key is null)
                        {
                            continue;
                        }

                        if (!config.Warnings.TryGetValue(keyResult.Key, out List<string>? warningList))
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
                    metadata = metadataResult.CheckedData;
                }

                metadata["used_layers"] = JsonConvert.SerializeObject(environment.Layers);
                metadata["stale"] = JsonConvert.SerializeObject(false);

                await _objectStore.StoreMetadata<PreparedConfiguration, ConfigurationIdentifier>(config, metadata);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "failed to compile configuration, see exception for more details");
            }
        }

        private async Task HandleDefaultEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<DefaultEnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                IsDefault = true
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentDeleted(IDomainEvent<EnvironmentDeleted> domainEvent)
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

            EnvironmentLayer source = sourceEnvironmentResult.CheckedData;
            var newLayer = new EnvironmentLayer(domainEvent.Payload.TargetIdentifier)
            {
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CurrentVersion = (long)eventHeader.EventNumber,
                Json = source.Json,
                Keys = source.Keys,
                KeyPaths = source.KeyPaths,
                Tags = source.Tags
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(newLayer);
        }

        private async Task HandleEnvironmentLayerCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerCreated> domainEvent)
        {
            var layer = new EnvironmentLayer(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);
        }

        private async Task HandleEnvironmentLayerDeleted(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerDeleted> domainEvent)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

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

            EnvironmentLayer layer = layerResult.CheckedData;
            List<ConfigKeyAction> impliedActions = layer.Keys
                                                        .Select(k => ConfigKeyAction.Delete(k.Key))
                                                        .ToList();

            // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
            // don't care about type of these lists/maps - just initialize them in empty-state
            // ---
            // create an empty version of the layer we're removing,
            // so we can trigger possible cleanup-actions with the current state (removed) of this layer
            EnvironmentLayer removedLayer = new(domainEvent.Payload.Identifier)
            {
                Json = string.Empty,
                Keys = new(),
                Tags = new(),
                // last change = now (deleted)
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                // keep original creation-info
                CreatedAt = layer.CreatedAt,
                CreatedBy = layer.CreatedBy,
                // version = now (delete-event)
                CurrentVersion = (long)eventHeader.EventNumber,
                KeyPaths = new()
            };
            // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

            await OnLayerKeysChanged(removedLayer, impliedActions, translator);
            await _objectStore.Remove<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);
        }

        private async Task HandleEnvironmentLayerKeysImported(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerKeysImported> domainEvent)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.CheckedData;

            // set the 'Version' property of all changed Keys to the current unix-timestamp for later use
            var keyVersion = (long)domainEvent.Timestamp
                                              .ToUniversalTime()
                                              .Subtract(DateTime.UnixEpoch)
                                              .TotalSeconds;

            // start with an empty list, because the import always overwrites whatever is there
            // so we only need to count SET-Actions and add them to the new list
            Dictionary<string, EnvironmentLayerKey> importedLayerKeys = new();
            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(a => a.Type == ConfigKeyActionType.Set))
            {
                // if we can find the new key with a different capitalization,
                // we remove the old and add the new one
                if (importedLayerKeys.Keys
                                     .FirstOrDefault(
                                         k => k.Equals(
                                             change.Key,
                                             StringComparison.OrdinalIgnoreCase))
                        is { } existingKey)
                {
                    importedLayerKeys.Remove(existingKey);
                }

                importedLayerKeys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            EnvironmentLayer importedLayer = new(layer.Id)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Json = translator.ToJson(
                                     importedLayerKeys.ToDictionary(
                                         kvp => kvp.Value.Key,
                                         kvp => kvp.Value.Value))
                                 .ToString(),
                Keys = importedLayerKeys,
                KeyPaths = GenerateKeyPaths(importedLayerKeys)
            };

            await OnLayerKeysChanged(importedLayer, domainEvent.Payload.ModifiedKeys, translator);
            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(importedLayer);
        }

        private async Task HandleEnvironmentLayerKeysModified(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerKeysModified> domainEvent)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var translator = scope.ServiceProvider.GetRequiredService<IJsonTranslator>();

            IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);

            if (layerResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify layer, but layer wasn't found in configured store: {ErrorCode} {Message}",
                    layerResult.Code,
                    layerResult.Message);
                return;
            }

            EnvironmentLayer layer = layerResult.CheckedData;
            Dictionary<string, EnvironmentLayerKey> modifiedKeys = layer.Keys;

            foreach (ConfigKeyAction deletion in domainEvent.Payload
                                                            .ModifiedKeys
                                                            .Where(action => action.Type == ConfigKeyActionType.Delete))
            {
                if (modifiedKeys.ContainsKey(deletion.Key))
                {
                    modifiedKeys.Remove(deletion.Key);
                }
            }

            // set the 'Version' property of all changed Keys to the current unix-timestamp for later use
            var keyVersion = (long)domainEvent.Timestamp
                                              .ToUniversalTime()
                                              .Subtract(DateTime.UnixEpoch)
                                              .TotalSeconds;

            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(a => a.Type == ConfigKeyActionType.Set))
            {
                // if we can find the new key with a different capitalization,
                // we remove the old and add the new one
                if (modifiedKeys.Select(k => k.Key)
                                .FirstOrDefault(k => k.Equals(change.Key, StringComparison.OrdinalIgnoreCase))
                        is { } existingKey)
                {
                    modifiedKeys.Remove(existingKey);
                }

                modifiedKeys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            EnvironmentLayer modifiedLayer = new(layer.Id)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Json = translator.ToJson(
                                     modifiedKeys.ToDictionary(
                                         kvp => kvp.Value.Key,
                                         kvp => kvp.Value.Value))
                                 .ToString(),
                Keys = modifiedKeys,
                KeyPaths = GenerateKeyPaths(modifiedKeys)
            };

            await OnLayerKeysChanged(modifiedLayer, domainEvent.Payload.ModifiedKeys, translator);
            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);
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

            ConfigEnvironment environment = envResult.CheckedData;

            IResult<Dictionary<string, EnvironmentLayerKey>> envDataResult = await ResolveEnvironmentKeys(domainEvent.Payload.Layers);
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

            ConfigEnvironment modifiedEnvironment = new(environment)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                CurrentVersion = (long)eventHeader.EventNumber,
                Json = translator.ToJson(envDataResult.CheckedData.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)).ToString(),
                Keys = envDataResult.CheckedData,
                KeyPaths = GenerateKeyPaths(envDataResult.CheckedData),
                Layers = domainEvent.Payload.Layers
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(modifiedEnvironment);
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

            EnvironmentLayer layer = layerResult.CheckedData;
            List<string> modifiedTags = layer.Tags;

            foreach (string tag in domainEvent.Payload.AddedTags.Where(tag => !modifiedTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                modifiedTags.Add(tag);
            }

            foreach (string tag in domainEvent.Payload.RemovedTags)
            {
                string? existingTag = modifiedTags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (existingTag is null)
                {
                    continue;
                }

                modifiedTags.Remove(existingTag);
            }

            EnvironmentLayer modifiedLayer = new(layer.Id)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Tags = modifiedTags,
                CurrentVersion = (long)eventHeader.EventNumber,
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);
        }

        private async Task HandleStructureCreated(StreamedEventHeader eventHeader, IDomainEvent<StructureCreated> domainEvent)
        {
            var structure = new ConfigStructure(domainEvent.Payload.Identifier)
            {
                Keys = domainEvent.Payload.Keys,
                Variables = domainEvent.Payload.Variables,
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CreatedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(structure);
        }

        private async Task HandleStructureDeleted(IDomainEvent<StructureDeleted> domainEvent)
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

            ConfigStructure structure = structureResult.CheckedData;
            Dictionary<string, string?> modifiedVariables = structure.Variables;

            foreach (ConfigKeyAction deletion in domainEvent.Payload
                                                            .ModifiedKeys
                                                            .Where(action => action.Type == ConfigKeyActionType.Delete))
            {
                if (modifiedVariables.ContainsKey(deletion.Key))
                {
                    modifiedVariables.Remove(deletion.Key);
                }
            }

            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(action => action.Type == ConfigKeyActionType.Set))
            {
                modifiedVariables[change.Key] = change.Value;
            }

            ConfigStructure modifiedStructure = new(structure)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                CurrentVersion = (long)eventHeader.EventNumber,
                Variables = modifiedVariables
            };

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(modifiedStructure);
        }

        private async Task OnLayerKeysChanged(EnvironmentLayer layer, ICollection<ConfigKeyAction> modifiedKeys, IJsonTranslator translator)
        {
            await UpdateEnvironmentProjections(layer, translator);
            await UpdateConfigurationStaleStatus(layer, modifiedKeys);
        }

        private async Task<IResult<Dictionary<string, EnvironmentLayerKey>>> ResolveEnvironmentKeys(IEnumerable<LayerIdentifier> layerIds)
        {
            var result = new Dictionary<string, EnvironmentLayerKey>(StringComparer.OrdinalIgnoreCase);

            foreach (LayerIdentifier layerId in layerIds)
            {
                IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(layerId);
                if (layerResult.IsError)
                {
                    return Result.Error<Dictionary<string, EnvironmentLayerKey>>(layerResult.Message, layerResult.Code);
                }

                EnvironmentLayer layer = layerResult.CheckedData;

                foreach ((string _, EnvironmentLayerKey entry) in layer.Keys)
                {
                    // remove existing key if it exists already...
                    if (result.ContainsKey(entry.Key))
                    {
                        result.Remove(entry.Key);
                    }

                    // add or replace key
                    result.Add(entry.Key, entry);
                }
            }

            return Result.Success(result);
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

            foreach (ConfigurationIdentifier configId in configIdResult.CheckedData.Items)
            {
                IResult<IDictionary<string, string>> metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(configId);
                if (metadataResult.IsError)
                {
                    _logger.LogWarning("unable to load metadata for config with id {Identifier} to update its stale-property", configId);
                    continue;
                }

                IDictionary<string, string> metadata = metadataResult.CheckedData;

                // no key? assume it's not stale
                bool isStale = metadata.ContainsKey("stale") && JsonConvert.DeserializeObject<bool>(metadata["stale"]);

                if (isStale)
                {
                    _logger.LogDebug("config with id {Identifier} is already marked as stale - skipping re-calculation of staleness", configId);
                    continue;
                }

                List<LayerIdentifier>? usedLayers = metadata.ContainsKey("used_layers")
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

                PreparedConfiguration config = configResult.CheckedData;

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

        private async Task UpdateEnvironmentProjections(EnvironmentLayer layer, IJsonTranslator translator)
        {
            IResult<Page<EnvironmentIdentifier>> envIdResult = await _objectStore.ListAll<ConfigEnvironment, EnvironmentIdentifier>(QueryRange.All);
            if (envIdResult.IsError)
            {
                return;
            }

            foreach (EnvironmentIdentifier envId in envIdResult.CheckedData.Items)
            {
                IResult<ConfigEnvironment> environmentResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(envId);
                if (environmentResult.IsError)
                {
                    continue;
                }

                ConfigEnvironment environment = environmentResult.CheckedData;
                if (!environment.Layers.Contains(layer.Id))
                {
                    continue;
                }

                IResult<Dictionary<string, EnvironmentLayerKey>> environmentDataResult = await ResolveEnvironmentKeys(environment.Layers);
                if (environmentDataResult.IsError)
                {
                    continue;
                }

                ConfigEnvironment modifiedEnvironment = new(environment)
                {
                    // updated with new layer-data
                    Keys = environmentDataResult.CheckedData,
                    Json = translator.ToJson(
                                         environmentDataResult.CheckedData.ToDictionary(
                                             kvp => kvp.Value.Key,
                                             kvp => kvp.Value.Value))
                                     .ToString(),
                    KeyPaths = GenerateKeyPaths(environmentDataResult.CheckedData),
                    CurrentVersion = layer.CurrentVersion
                };

                await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(modifiedEnvironment);
            }
        }
    }
}
