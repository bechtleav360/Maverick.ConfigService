using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.EventStore.DomainEventBase;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Default-Implementation of <see cref="IDomainObjectManager" />
    /// </summary>
    public class DomainObjectManager : IDomainObjectManager
    {
        private readonly IEventStore _eventStore;
        private readonly EventStoreConnectionConfiguration _eventStoreConfiguration;
        private readonly ILogger<DomainObjectManager> _logger;
        private readonly IDomainObjectStore _objectStore;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectManager" />
        /// </summary>
        /// <param name="objectStore">instance to store/load DomainObjects to/from</param>
        /// <param name="eventStore">IEventStore to write new domain-events to</param>
        /// <param name="eventStoreConfiguration">eventStore-configuration</param>
        /// <param name="logger"></param>
        public DomainObjectManager(
            IDomainObjectStore objectStore,
            IEventStore eventStore,
            EventStoreConnectionConfiguration eventStoreConfiguration,
            ILogger<DomainObjectManager> logger)
        {
            _objectStore = objectStore;
            _eventStore = eventStore;
            _eventStoreConfiguration = eventStoreConfiguration;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<IResult> AssignEnvironmentLayers(
            EnvironmentIdentifier environmentIdentifier,
            IList<LayerIdentifier> layerIdentifiers,
            CancellationToken cancellationToken)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResult> CreateConfiguration(
            ConfigurationIdentifier identifier,
            DateTime? validFrom,
            DateTime? validTo,
            CancellationToken cancellationToken)
            => CreateObject<PreparedConfiguration, ConfigurationIdentifier>(
                identifier,
                new List<IDomainEvent> {new DomainEvent<ConfigurationBuilt>("Anonymous", new ConfigurationBuilt(identifier, validFrom, validTo))},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => CreateObject<ConfigEnvironment, EnvironmentIdentifier>(
                identifier,
                new List<IDomainEvent> {new DomainEvent<EnvironmentCreated>("Anonymous", new EnvironmentCreated(identifier))},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => CreateObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<IDomainEvent> {new DomainEvent<EnvironmentLayerCreated>("Anonymous", new EnvironmentLayerCreated(identifier))},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables,
            CancellationToken cancellationToken)
            => CreateObject<ConfigStructure, StructureIdentifier>(
                identifier,
                new List<IDomainEvent> {new DomainEvent<StructureCreated>("Anonymous", new StructureCreated(identifier, keys, variables))},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => DeleteObject<ConfigEnvironment, EnvironmentIdentifier>(
                identifier,
                new DomainEvent<EnvironmentDeleted>("Anonymous", new EnvironmentDeleted(identifier)),
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => DeleteObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new DomainEvent<EnvironmentLayerDeleted>("Anonymous", new EnvironmentLayerDeleted(identifier)),
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<PreparedConfiguration, ConfigurationIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetConfigurationKeys(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
        {
            IResult<PreparedConfiguration> result = await LoadObject<PreparedConfiguration, ConfigurationIdentifier>(identifier, cancellationToken);
            if (!result.IsError)
            {
                return Result.Success(result.Data.Keys);
            }

            _logger.LogWarning(
                "unable to load configuration with id '{Identifier}': {ErrorCode} {Message}",
                identifier,
                result.Code,
                result.Message);

            return Result.Error<IDictionary<string, string>>(result.Message, result.Code);
        }

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigEnvironment, EnvironmentIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentLayerKey>>> GetLayerKeys(LayerIdentifier identifier, CancellationToken cancellationToken)
        {
            IResult<EnvironmentLayer> result = await LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);
            if (!result.IsError)
            {
                return Result.Success<IList<EnvironmentLayerKey>>(
                    result.Data
                          .Keys
                          .Select(kvp => kvp.Value)
                          .ToList());
            }

            _logger.LogWarning(
                "unable to load layer with id '{Identifier}': {ErrorCode} {Message}",
                identifier,
                result.Code,
                result.Message);

            return Result.Error<IList<EnvironmentLayerKey>>(result.Message, result.Code);
        }

        /// <inheritdoc />
        public Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigStructure, StructureIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
        {
            IResult<EnvironmentLayer> result = await LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);
            if (result.IsError)
            {
                _logger.LogWarning(
                    "unable to load layer with id '{Identifier}': {ErrorCode} {Message}",
                    identifier,
                    result.Code,
                    result.Message);
                return result;
            }

            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected, to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;

            await _eventStore.WriteEventsAsync(
                new List<IDomainEvent>
                {
                    new DomainEvent<EnvironmentLayerKeysModified>(
                        "Anonymous",
                        new EnvironmentLayerKeysModified(
                            identifier,
                            actions.ToArray()))
                },
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
        {
            IResult<ConfigStructure> result = await LoadObject<ConfigStructure, StructureIdentifier>(identifier, cancellationToken);
            if (result.IsError)
            {
                _logger.LogWarning(
                    "unable to load structure with id '{Identifier}': {ErrorCode} {Message}",
                    identifier,
                    result.Code,
                    result.Message);
                return result;
            }

            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected, to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;

            await _eventStore.WriteEventsAsync(
                new List<IDomainEvent>
                {
                    new DomainEvent<StructureVariablesModified>(
                        "Anonymous",
                        new StructureVariablesModified(
                            identifier,
                            actions.ToArray()))
                },
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        private async Task<IResult> CreateObject<TObject, TIdentifier>(
            TIdentifier identifier,
            IList<IDomainEvent> createEvents,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            IResult<TObject> result = await LoadObject<TObject, TIdentifier>(identifier, cancellationToken);
            // thing is already created, act as if we did our job
            if (!result.IsError)
            {
                _logger.LogDebug(
                    "skipping creation of {DomainObject} with id {Identifier} because it already exists",
                    typeof(TObject).Name,
                    identifier);
                return Result.Success();
            }

            // thing couldn't be found, but not because it doesn't exist
            // this means there is an actual problem and we can't create the thing
            if (result.Code != ErrorCode.NotFound)
            {
                _logger.LogWarning(
                    "unable to create object {DomainObject} with id {Identifier}: {ErrorCode} {Message}",
                    typeof(TObject).Name,
                    identifier,
                    result.Code,
                    result.Message);
                return Result.Error(result.Message, result.Code);
            }

            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected, to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;

            await _eventStore.WriteEventsAsync(
                createEvents,
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        private async Task<IResult> DeleteObject<TObject, TIdentifier>(TIdentifier identifier, IDomainEvent deleteEvent, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            // check if the environment actually exists
            IResult<TObject> objectResult = await LoadObject<TObject, TIdentifier>(identifier, cancellationToken);
            if (objectResult.IsError)
            {
                return objectResult;
            }

            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected, to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;

            await _eventStore.WriteEventsAsync(
                new List<IDomainEvent> {deleteEvent},
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        private async Task<IResult<TObject>> LoadObject<TObject, TIdentifier>(TIdentifier identifier, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            IResult<TObject> result = await _objectStore.Load<TObject, TIdentifier>(identifier);

            if (!result.IsError)
            {
                return Result.Success(result.Data);
            }

            _logger.LogWarning(
                "unable to load domain-object '{DomainObject}' with id '{Identifier}': {ErrorCode} {Message}",
                typeof(TObject).Name,
                identifier,
                result.Code,
                result.Message);

            return result;
        }
    }
}
