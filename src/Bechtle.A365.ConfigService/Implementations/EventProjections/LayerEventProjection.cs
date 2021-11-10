using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations.EventProjections
{
    /// <summary>
    ///     Projection for all DomainEvents regarding <see cref="EnvironmentLayer" />
    /// </summary>
    public class LayerEventProjection :
        EventProjectionBase,
        IDomainEventProjection<EnvironmentLayerCreated>,
        IDomainEventProjection<EnvironmentLayerDeleted>,
        IDomainEventProjection<EnvironmentLayerCopied>,
        IDomainEventProjection<EnvironmentLayerTagsChanged>,
        IDomainEventProjection<EnvironmentLayerKeysImported>,
        IDomainEventProjection<EnvironmentLayerKeysModified>
    {
        private readonly ILogger<LayerEventProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IJsonTranslator _translator;

        /// <summary>
        ///     Create a new instance of <see cref="LayerEventProjection"/>
        /// </summary>
        /// <param name="objectStore">storage for generated configs</param>
        /// <param name="translator">translator to generate json-views</param>
        /// <param name="logger">logger to write diagnostic information</param>
        public LayerEventProjection(
            IDomainObjectStore objectStore,
            IJsonTranslator translator,
            ILogger<LayerEventProjection> logger)
            : base(objectStore)
        {
            _objectStore = objectStore;
            _translator = translator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerCopied> domainEvent)
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
            var newLayer = new EnvironmentLayer(source)
            {
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CurrentVersion = (long)eventHeader.EventNumber,
                Id = domainEvent.Payload.TargetIdentifier
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(newLayer);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerCreated> domainEvent)
        {
            var layer = new EnvironmentLayer(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerDeleted> domainEvent)
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

            await _objectStore.Remove<EnvironmentLayer, LayerIdentifier>(domainEvent.Payload.Identifier);
            await OnLayerKeysChanged(removedLayer, impliedActions, _translator);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerKeysImported> domainEvent)
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

            EnvironmentLayer importedLayer = new(layer)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Json = _translator.ToJson(
                                      importedLayerKeys.ToDictionary(
                                          kvp => kvp.Value.Key,
                                          kvp => kvp.Value.Value))
                                  .ToString(),
                Keys = importedLayerKeys,
                KeyPaths = GenerateKeyPaths(importedLayerKeys)
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(importedLayer);
            await OnLayerKeysChanged(importedLayer, domainEvent.Payload.ModifiedKeys, _translator);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerKeysModified> domainEvent)
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

            EnvironmentLayer modifiedLayer = new(layer)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Json = _translator.ToJson(
                                      modifiedKeys.ToDictionary(
                                          kvp => kvp.Value.Key,
                                          kvp => kvp.Value.Value))
                                  .ToString(),
                Keys = modifiedKeys,
                KeyPaths = GenerateKeyPaths(modifiedKeys)
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);
            await OnLayerKeysChanged(modifiedLayer, domainEvent.Payload.ModifiedKeys, _translator);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayerTagsChanged> domainEvent)
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

            EnvironmentLayer modifiedLayer = new(layer)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                Tags = modifiedTags,
                CurrentVersion = (long)eventHeader.EventNumber
            };

            await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);
        }

        private async Task OnLayerKeysChanged(
            EnvironmentLayer layer,
            ICollection<ConfigKeyAction> modifiedKeys,
            IJsonTranslator translator)
        {
            await UpdateEnvironmentProjections(layer, translator);
            await UpdateConfigurationStaleStatus(layer, modifiedKeys);
        }

        // @TODO: un-/assignment of Layers in Environment don't currently mark a Configuration as Stale
        private async Task UpdateConfigurationStaleStatus(
            EnvironmentLayer layer,
            ICollection<ConfigKeyAction> modifiedKeys)
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

        private async Task UpdateEnvironmentProjections(
            EnvironmentLayer layer,
            IJsonTranslator translator)
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
