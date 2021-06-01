﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.Services.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Component that receives
    /// </summary>
    public class DomainObjectProjection : EventSubscriptionBase
    {
        private readonly IOptionsSnapshot<EventStoreConnectionConfiguration> _configuration;
        private readonly ILogger<DomainObjectProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectProjection" />
        /// </summary>
        /// <param name="eventStore">EventStore that handles the underlying subscription</param>
        /// <param name="objectStore">store for the projected DomainObjects</param>
        /// <param name="compiler">compiler to build configurations when <see cref="ConfigurationBuilt" /> is handled</param>
        /// <param name="parser">parser for compilation-process</param>
        /// <param name="translator">json-translator for the compilation-process</param>
        /// <param name="configuration">options used to configure the subscription</param>
        /// <param name="logger">logger to write information to</param>
        public DomainObjectProjection(
            IEventStore eventStore,
            IDomainObjectStore objectStore,
            IConfigurationCompiler compiler,
            IConfigurationParser parser,
            IJsonTranslator translator,
            IOptionsSnapshot<EventStoreConnectionConfiguration> configuration,
            ILogger<DomainObjectProjection> logger) : base(eventStore)
        {
            _objectStore = objectStore;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task OnDomainEventReceived(StreamedEventHeader eventHeader, IDomainEvent domainEvent)
        {
            Task task = domainEvent switch
            {
                IDomainEvent<ConfigurationBuilt> e => HandleConfigurationBuilt(eventHeader, e),
                IDomainEvent<DefaultEnvironmentCreated> e => HandleDefaultEnvironmentCreated(eventHeader, e),
                IDomainEvent<EnvironmentCreated> e => HandleEnvironmentCreated(eventHeader, e),
                IDomainEvent<EnvironmentDeleted> e => HandleEnvironmentDeleted(eventHeader, e),
                IDomainEvent<EnvironmentLayerCreated> e => HandleEnvironmentLayerCreated(eventHeader, e),
                IDomainEvent<EnvironmentLayerDeleted> e => HandleEnvironmentLayerDeleted(eventHeader, e),
                IDomainEvent<EnvironmentLayerKeysImported> e => HandleEnvironmentLayerKeysImported(eventHeader, e),
                IDomainEvent<EnvironmentLayerKeysModified> e => HandleEnvironmentLayerKeysModified(eventHeader, e),
                IDomainEvent<StructureCreated> e => HandleStructureCreated(eventHeader, e),
                IDomainEvent<StructureDeleted> e => HandleStructureDeleted(eventHeader, e),
                IDomainEvent<StructureVariablesModified> e => HandleStructureVariablesModified(eventHeader, e),
                _ => Task.CompletedTask
            };

            await task;
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
        }

        private async Task HandleConfigurationBuilt(StreamedEventHeader eventHeader, IDomainEvent<ConfigurationBuilt> domainEvent)
        {
            var config = new PreparedConfiguration(domainEvent.Payload.Identifier)
            {
                ConfigurationVersion = (long) domainEvent.Timestamp
                                                         .Subtract(DateTime.UnixEpoch)
                                                         .TotalSeconds,
                CurrentVersion = (long) eventHeader.EventNumber,
                MetaVersion = (long) eventHeader.EventNumber,
                ValidFrom = domainEvent.Payload.ValidFrom,
                ValidTo = domainEvent.Payload.ValidTo
            };

            IResult compilationResult = await Compile(config);

            await _objectStore.Store<PreparedConfiguration, ConfigurationIdentifier>(config);
        }

        private async Task HandleDefaultEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<DefaultEnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier);

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier);

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        private async Task HandleEnvironmentDeleted(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentDeleted> domainEvent)
        {
            await _objectStore.Remove<ConfigEnvironment, EnvironmentIdentifier>(domainEvent.Payload.Identifier);
        }

        private async Task HandleEnvironmentLayerCreated(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerCreated> domainEvent)
        {
            var layer = new EnvironmentLayer(domainEvent.Payload.Identifier);

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);
        }

        private async Task HandleEnvironmentLayerDeleted(StreamedEventHeader eventHeader, IDomainEvent<EnvironmentLayerDeleted> domainEvent)
        {
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
                                                          .Where(a => a.Type == ConfigKeyActionType.Delete))
            {
                if (layer.Keys.ContainsKey(change.Key))
                {
                    layer.Keys.Remove(change.Key);
                }

                layer.Keys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            await OnLayerKeysChanged(layer);
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
                                                          .Where(a => a.Type == ConfigKeyActionType.Delete))
            {
                if (layer.Keys.ContainsKey(change.Key))
                {
                    layer.Keys.Remove(change.Key);
                }

                layer.Keys[change.Key] = new EnvironmentLayerKey(
                    change.Key,
                    change.Value,
                    change.ValueType,
                    change.Description,
                    keyVersion);
            }

            await OnLayerKeysChanged(layer);
        }

        private async Task<IResult> OnLayerKeysChanged(EnvironmentLayer layer)
        {
            layer.KeyPaths = GenerateKeyPaths(layer.Keys);
            layer.Json = _translator.ToJson(layer.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Key))
                                    .ToString();

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);

            IResult<IList<EnvironmentIdentifier>> envIdResult = await _objectStore.ListAll<ConfigEnvironment, EnvironmentIdentifier>(QueryRange.All);
            if (envIdResult.IsError)
            {
                return envIdResult;
            }

            foreach (EnvironmentIdentifier envId in envIdResult.Data)
            {
                IResult<ConfigEnvironment> environmentResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(envId);
                if (environmentResult.IsError)
                {
                    return environmentResult;
                }

                ConfigEnvironment environment = environmentResult.Data;
                if (!environment.Layers.Contains(layer.Id))
                {
                    continue;
                }

                IResult<Dictionary<string, EnvironmentLayerKey>> environmentDataResult = await ResolveEnvironmentKeys(environment);
                if (environmentDataResult.IsError)
                {
                    return environmentDataResult;
                }

                environment.Keys = environmentDataResult.Data;
                environment.Json = _translator.ToJson(environment.Keys.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)).ToString();
                environment.KeyPaths = GenerateKeyPaths(environment.Keys);

                await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
            }

            return Result.Success();
        }

        private async Task HandleStructureCreated(StreamedEventHeader eventHeader, IDomainEvent<StructureCreated> domainEvent)
        {
            var structure = new ConfigStructure(domainEvent.Payload.Identifier);

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
        }

        /// <summary>
        ///     Compile the configuration that this object represents
        /// </summary>
        /// <param name="config">config to compile and update</param>
        /// <returns>Result of the operation</returns>
        private async Task<IResult> Compile(PreparedConfiguration config)
        {
            _logger?.LogDebug($"version used during compilation: {config.CurrentVersion}");

            IResult<ConfigEnvironment> envResult = await _objectStore.Load<ConfigEnvironment, EnvironmentIdentifier>(config.Id.Environment);
            if (envResult.IsError)
            {
                return envResult;
            }

            IResult<ConfigStructure> structResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(config.Id.Structure);
            if (structResult.IsError)
            {
                return structResult;
            }

            ConfigEnvironment environment = envResult.Data;
            ConfigStructure structure = structResult.Data;

            try
            {
                IResult<Dictionary<string, EnvironmentLayerKey>> environmentDataResult = await ResolveEnvironmentKeys(environment);
                if (environmentDataResult.IsError)
                {
                    return environmentDataResult;
                }

                Dictionary<string, EnvironmentLayerKey> environmentData = environmentDataResult.Data;

                CompilationResult compilationResult = _compiler.Compile(
                    new EnvironmentCompilationInfo
                    {
                        Name = $"{config.Id.Environment.Category}/{config.Id.Environment.Name}",
                        Keys = environmentData.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)
                    },
                    new StructureCompilationInfo
                    {
                        Name = $"{config.Id.Structure.Name}/{config.Id.Structure.Version}",
                        Keys = structure.Keys,
                        Variables = structure.Variables
                    },
                    _parser);

                config.Keys = compilationResult.CompiledConfiguration;
                config.Json = _translator.ToJson(config.Keys).ToString();
                config.UsedKeys = compilationResult.GetUsedKeys().ToList();

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "failed to compile configuration, see exception for more details");
                return Result.Error($"failed to compile configuration: {e.Message}", ErrorCode.InvalidData);
            }
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
    }
}
