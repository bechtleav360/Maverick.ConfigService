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

namespace Bechtle.A365.ConfigService.Implementations.EventProjections
{
    /// <summary>
    ///     Projection for all DomainEvents regarding <see cref="ConfigEnvironment" />
    /// </summary>
    public class EnvironmentEventProjection :
        EventProjectionBase,
        IDomainEventProjection<DefaultEnvironmentCreated>,
        IDomainEventProjection<EnvironmentCreated>,
        IDomainEventProjection<EnvironmentDeleted>,
        IDomainEventProjection<EnvironmentLayersModified>
    {
        private readonly ILogger<EnvironmentEventProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IJsonTranslator _translator;

        /// <summary>
        ///     Create a new instance of <see cref="EnvironmentEventProjection" />
        /// </summary>
        /// <param name="objectStore">storage for generated configs</param>
        /// <param name="translator">translator to generate json-views</param>
        /// <param name="logger">logger to write diagnostic information</param>
        public EnvironmentEventProjection(
            IDomainObjectStore objectStore,
            IJsonTranslator translator,
            ILogger<EnvironmentEventProjection> logger)
            : base(objectStore)
        {
            _objectStore = objectStore;
            _logger = logger;
            _translator = translator;
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<DefaultEnvironmentCreated> domainEvent)
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

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentCreated> domainEvent)
        {
            var environment = new ConfigEnvironment(domainEvent.Payload.Identifier)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                CreatedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(environment);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentDeleted> domainEvent)
        {
            await _objectStore.Remove<ConfigEnvironment, EnvironmentIdentifier>(domainEvent.Payload.Identifier);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayersModified> domainEvent)
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

            // first, resolve the env-keys and then update the actual environment
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

            ConfigEnvironment modifiedEnvironment = new(environment)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CurrentVersion = (long)eventHeader.EventNumber,
                Json = _translator.ToJson(
                                      envDataResult.CheckedData
                                                   .ToDictionary(
                                                       kvp => kvp.Value.Key,
                                                       kvp => kvp.Value.Value))
                                  .ToString(),
                Keys = envDataResult.CheckedData,
                KeyPaths = GenerateKeyPaths(envDataResult.CheckedData),
                Layers = domainEvent.Payload.Layers
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(modifiedEnvironment);

            // second, update all layers to show they're assigned to this environment
            await LayersAssignedToEnvironment(
                eventHeader,
                domainEvent,
                domainEvent.Payload.Layers,
                environment);
        }

        private async Task AddUsedEnvironmentToList(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayersModified> domainEvent,
            ConfigEnvironment environment,
            EnvironmentLayer layer)
        {
            _logger.LogInformation(
                "updating {Layer} to show it's assigned to {Environment}",
                layer,
                environment.Id);

            EnvironmentLayer modifiedLayer = new(layer)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                UsedInEnvironments = layer.UsedInEnvironments
                                          .Append(environment.Id)
                                          .ToList()
            };

            IResult result = await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "unable to mark {Layer} as assigned to {Environment}: {ErrorCode} {Message}",
                    layer.Id,
                    environment.Id,
                    result.Code,
                    result.Message);
            }
        }

        private async Task LayersAssignedToEnvironment(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayersModified> domainEvent,
            ICollection<LayerIdentifier> layers,
            ConfigEnvironment environment)
        {
            _logger.LogInformation(
                "updating {LayerCount} layers to show they're assigned to {Environment}",
                layers.Count,
                environment.Id);

            IResult<Page<LayerIdentifier>> layerIds =
                await _objectStore.ListAll<EnvironmentLayer, LayerIdentifier>(QueryRange.All);

            if (layerIds.IsError)
            {
                _logger.LogError("unable to enumerate layers to update their environment-assignments");
                return;
            }

            Page<LayerIdentifier> allLayerIds = layerIds.CheckedData;

            foreach (LayerIdentifier layerId in allLayerIds)
            {
                IResult<EnvironmentLayer> layerResult =
                    await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(layerId);

                if (layerResult.IsError)
                {
                    _logger.LogError(
                        "unable to update layer {Layer} before assigning it to {Environment}: {ErrorCode} {Message}",
                        layerId,
                        environment.Id,
                        layerResult.Code,
                        layerResult.Message);
                    continue;
                }

                EnvironmentLayer layer = layerResult.CheckedData;

                // layer should be marked as assigned to environment
                if (layers.Contains(layer.Id))
                {
                    // layer is not marked as assigned to environment => add it
                    if (!layer.UsedInEnvironments.Contains(environment.Id))
                    {
                        await AddUsedEnvironmentToList(eventHeader, domainEvent, environment, layer);
                    }
                }
                // layer should *not* be marked as assigned to environment
                else
                {
                    // layer is still marked as assigned => remove it
                    if (layer.UsedInEnvironments.Contains(environment.Id))
                    {
                        await RemoveUsedEnvironmentFromList(eventHeader, domainEvent, environment, layer);
                    }
                }
            }
        }

        private async Task RemoveUsedEnvironmentFromList(
            StreamedEventHeader eventHeader,
            IDomainEvent<EnvironmentLayersModified> domainEvent,
            ConfigEnvironment environment,
            EnvironmentLayer layer)
        {
            _logger.LogInformation(
                "updating {Layer} to show it's not assigned to {Environment} anymore",
                layer,
                environment.Id);

            EnvironmentLayer modifiedLayer = new(layer)
            {
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                UsedInEnvironments = layer.UsedInEnvironments
                                          .Where(id => id != environment.Id)
                                          .ToList()
            };

            IResult result = await _objectStore.Store<EnvironmentLayer, LayerIdentifier>(modifiedLayer);

            if (result.IsError)
            {
                _logger.LogWarning(
                    "unable to mark {Layer} as assigned to {Environment}: {ErrorCode} {Message}",
                    layer.Id,
                    environment.Id,
                    result.Code,
                    result.Message);
            }
        }
    }
}
