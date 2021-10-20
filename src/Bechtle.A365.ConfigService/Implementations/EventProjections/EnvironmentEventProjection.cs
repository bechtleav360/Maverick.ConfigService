using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
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
        ///     Create a new instance of <see cref="EnvironmentEventProjection"/>
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
                ChangedBy = "Anonymous",
                CurrentVersion = (long)eventHeader.EventNumber,
                Json = _translator.ToJson(envDataResult.CheckedData.ToDictionary(kvp => kvp.Value.Key, kvp => kvp.Value.Value)).ToString(),
                Keys = envDataResult.CheckedData,
                KeyPaths = GenerateKeyPaths(envDataResult.CheckedData),
                Layers = domainEvent.Payload.Layers
            };

            await _objectStore.Store<ConfigEnvironment, EnvironmentIdentifier>(modifiedEnvironment);
        }
    }
}
