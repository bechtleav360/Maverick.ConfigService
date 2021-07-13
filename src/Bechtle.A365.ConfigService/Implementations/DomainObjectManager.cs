using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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
        private readonly IList<ICommandValidator> _validators;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectManager" />
        /// </summary>
        /// <param name="objectStore">instance to store/load DomainObjects to/from</param>
        /// <param name="eventStore">IEventStore to write new domain-events to</param>
        /// <param name="eventStoreConfiguration">eventStore-configuration</param>
        /// <param name="validators">list of domain-event-validators, to use for validating generated Domain-Events</param>
        /// <param name="logger"></param>
        public DomainObjectManager(
            IDomainObjectStore objectStore,
            IEventStore eventStore,
            IOptionsSnapshot<EventStoreConnectionConfiguration> eventStoreConfiguration,
            IEnumerable<ICommandValidator> validators,
            ILogger<DomainObjectManager> logger)
        {
            _objectStore = objectStore;
            _eventStore = eventStore;
            _eventStoreConfiguration = eventStoreConfiguration.Value;
            _validators = validators.ToList();
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<IResult> AssignEnvironmentLayers(
            EnvironmentIdentifier environmentIdentifier,
            IList<LayerIdentifier> layerIdentifiers,
            CancellationToken cancellationToken)
            => ModifyObject<ConfigEnvironment, EnvironmentIdentifier>(
                environmentIdentifier,
                new List<DomainEvent> {new EnvironmentLayersModified(environmentIdentifier, layerIdentifiers.ToList())},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateConfiguration(
            ConfigurationIdentifier identifier,
            DateTime? validFrom,
            DateTime? validTo,
            CancellationToken cancellationToken)
            => CreateObject<PreparedConfiguration, ConfigurationIdentifier>(
                identifier,
                new List<DomainEvent> {new ConfigurationBuilt(identifier, validFrom, validTo)},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => CreateEnvironment(identifier, false, cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, bool isDefault, CancellationToken cancellationToken)
            => CreateObject<ConfigEnvironment, EnvironmentIdentifier>(
                identifier,
                new List<DomainEvent>
                {
                    isDefault
                        ? new DefaultEnvironmentCreated(identifier)
                        : (DomainEvent) new EnvironmentCreated(identifier)
                },
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => CreateObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> {new EnvironmentLayerCreated(identifier)},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables,
            CancellationToken cancellationToken)
            => CreateObject<ConfigStructure, StructureIdentifier>(
                identifier,
                new List<DomainEvent> {new StructureCreated(identifier, keys, variables)},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => DeleteObject<ConfigEnvironment, EnvironmentIdentifier>(
                identifier,
                new List<DomainEvent> {new EnvironmentDeleted(identifier)},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => DeleteObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> {new EnvironmentLayerDeleted(identifier)},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<PreparedConfiguration, ConfigurationIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(
            EnvironmentIdentifier environment,
            QueryRange range,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(c => c.Id.Environment == environment, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<ConfigurationIdentifier>>> GetConfigurations(
            StructureIdentifier structure,
            QueryRange range,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(c => c.Id.Structure == structure, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigEnvironment, EnvironmentIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<EnvironmentIdentifier>>> GetEnvironments(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigEnvironment, EnvironmentIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<LayerIdentifier>>> GetLayers(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<EnvironmentLayer, LayerIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetStaleConfigurations(QueryRange range, CancellationToken cancellationToken)
        {
            IResult<IList<ConfigurationIdentifier>> configIdResult =
                await ListObjects<PreparedConfiguration, ConfigurationIdentifier>(QueryRange.All, CancellationToken.None);

            if (configIdResult.IsError)
            {
                _logger.LogWarning(
                    "unable to query stale configs - unable to list all configurations to check their stale-ness: {Code} {Message}",
                    configIdResult.Code,
                    configIdResult.Message);
                return Result.Error<IList<ConfigurationIdentifier>>(configIdResult.Message, configIdResult.Code);
            }

            var staleConfigurationIds = new List<ConfigurationIdentifier>();
            foreach (ConfigurationIdentifier configId in configIdResult.Data)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                IResult<IDictionary<string, string>> metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(configId);
                if (metadataResult.IsError)
                {
                    _logger.LogWarning(
                        "unable to query stale configs - unable to get metadata for {Identifier}: {Code} {Message}",
                        configId,
                        configIdResult.Code,
                        configIdResult.Message);
                    return Result.Error<IList<ConfigurationIdentifier>>(configIdResult.Message, configIdResult.Code);
                }

                IDictionary<string, string> metadata = metadataResult.Data;
                bool isConfigStale = metadata.ContainsKey("stale") && JsonConvert.DeserializeObject<bool>(metadata["stale"]);

                if (isConfigStale)
                {
                    staleConfigurationIds.Add(configId);
                }
            }

            // paginate at the end when we have the full list, so the returned ids will be stable across multiple requests
            IList<ConfigurationIdentifier> pagedList = staleConfigurationIds.OrderBy(id => id.ToString())
                                                                            .Skip(range.Offset)
                                                                            .Take(range.Length)
                                                                            .ToList();

            return Result.Success(pagedList);
        }

        /// <inheritdoc />
        public Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigStructure, StructureIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<StructureIdentifier>>> GetStructures(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<IList<StructureIdentifier>>> GetStructures(string name, QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(s => s.Id.Name == name, range, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult> ImportLayer(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken)
        {
            var events = new List<IDomainEvent>();

            IResult<EnvironmentLayer> result = await LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);
            if (result.IsError)
            {
                if (result.Code == ErrorCode.NotFound)
                {
                    events.Add(
                        new DomainEvent<EnvironmentLayerCreated>(
                            "Anonymous",
                            new EnvironmentLayerCreated(identifier)));
                }
                else
                {
                    _logger.LogWarning(
                        "unable to load layer with id '{Identifier}': {ErrorCode} {Message}",
                        identifier,
                        result.Code,
                        result.Message);
                    return result;
                }
            }

            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected, to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;
            ConfigKeyAction[] actions = keys.Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                            .ToArray();
            events.Add(
                new DomainEvent<EnvironmentLayerKeysImported>(
                    "Anonymous",
                    new EnvironmentLayerKeysImported(
                        identifier,
                        actions)));

            await _eventStore.WriteEventsAsync(
                events,
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier)
        {
            IResult<IDictionary<string, string>> metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(identifier);

            if (metadataResult.IsError)
                return Result.Error<bool>(metadataResult.Message, metadataResult.Code);

            IDictionary<string, string> metadata = metadataResult.Data;
            if (!metadata.TryGetValue("stale", out string staleProperty))
            {
                return Result.Success(false);
            }

            var stale = JsonConvert.DeserializeObject<bool>(staleProperty);
            return Result.Success(stale);
        }

        /// <inheritdoc />
        public Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => ModifyObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> {new EnvironmentLayerKeysModified(identifier, actions.ToArray())},
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => ModifyObject<ConfigStructure, StructureIdentifier>(
                identifier,
                new List<DomainEvent> {new StructureVariablesModified(identifier, actions.ToArray())},
                cancellationToken);

        private async Task<IResult> CreateObject<TObject, TIdentifier>(
            TIdentifier identifier,
            IList<DomainEvent> createEvents,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            _logger.LogDebug("validating resulting events");
            IDictionary<DomainEvent, IList<IResult>> errors = Validate(_validators, createEvents);
            if (errors.Any())
            {
                return Result.Error(
                    "failed to validate generated DomainEvents",
                    ErrorCode.ValidationFailed,
                    errors.Values
                          .SelectMany(_ => _)
                          .ToList());
            }

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

            // convert *our*-type of DomainEvent to the generic late-binding one
            IList<IDomainEvent> domainEvents = createEvents.Select(e => (IDomainEvent) new LateBindingDomainEvent<DomainEvent>("Anonymous", e))
                                                           .ToList();

            await _eventStore.WriteEventsAsync(
                domainEvents,
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        private async Task<IResult> DeleteObject<TObject, TIdentifier>(
            TIdentifier identifier,
            IList<DomainEvent> deleteEvents,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            _logger.LogDebug("validating resulting events");
            IDictionary<DomainEvent, IList<IResult>> errors = Validate(_validators, deleteEvents);
            if (errors.Any())
            {
                return Result.Error(
                    "failed to validate generated DomainEvents",
                    ErrorCode.ValidationFailed,
                    errors.Values
                          .SelectMany(_ => _)
                          .ToList());
            }

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

            // convert *our*-type of DomainEvent to the generic late-binding one
            IList<IDomainEvent> domainEvents = deleteEvents.Select(e => (IDomainEvent) new LateBindingDomainEvent<DomainEvent>("Anonymous", e))
                                                           .ToList();

            await _eventStore.WriteEventsAsync(
                domainEvents,
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        private async Task<IResult<IList<TIdentifier>>> ListObjects<TObject, TIdentifier>(QueryRange range, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            IResult<IList<TIdentifier>> result = await _objectStore.ListAll<TObject, TIdentifier>(range);

            if (!result.IsError)
            {
                return Result.Success(result.Data);
            }

            _logger.LogWarning(
                "unable to list instances of domain-object '{DomainObject}': {ErrorCode} {Message}",
                typeof(TObject).Name,
                result.Code,
                result.Message);

            return result;
        }

        private async Task<IResult<IList<TIdentifier>>> ListObjects<TObject, TIdentifier>(
            Expression<Func<TObject, bool>> filter,
            QueryRange range,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            IResult<IList<TIdentifier>> result = await _objectStore.ListAll<TObject, TIdentifier>(filter, range);

            if (!result.IsError)
            {
                return Result.Success(result.Data);
            }

            _logger.LogWarning(
                "unable to list instances of domain-object '{DomainObject}': {ErrorCode} {Message}",
                typeof(TObject).Name,
                result.Code,
                result.Message);

            return result;
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

        private async Task<IResult> ModifyObject<TObject, TIdentifier>(
            TIdentifier identifier,
            IList<DomainEvent> modificationEvents,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            _logger.LogDebug("validating resulting events");
            IDictionary<DomainEvent, IList<IResult>> errors = Validate(_validators, modificationEvents);
            if (errors.Any())
            {
                return Result.Error(
                    "failed to validate generated DomainEvents",
                    ErrorCode.ValidationFailed,
                    errors.Values
                          .SelectMany(_ => _)
                          .ToList());
            }

            IResult<TObject> result = await LoadObject<TObject, TIdentifier>(identifier, cancellationToken);
            if (result.IsError)
            {
                _logger.LogWarning(
                    "unable to load {DomainObject} with id '{Identifier}': {ErrorCode} {Message}",
                    typeof(TObject).Name,
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

            // convert *our*-type of DomainEvent to the generic late-binding one
            IList<IDomainEvent> domainEvents = modificationEvents.Select(e => (IDomainEvent) new LateBindingDomainEvent<DomainEvent>("Anonymous", e))
                                                                 .ToList();

            await _eventStore.WriteEventsAsync(
                domainEvents,
                _eventStoreConfiguration.Stream,
                ExpectRevision.AtPosition(StreamPosition.Revision((ulong) lastProjectedEvent)));

            return Result.Success();
        }

        /// <summary>
        ///     validate all recorded events with the given <see cref="ICommandValidator" />
        /// </summary>
        /// <param name="validators">validators used to validate <paramref name="domainEvents" /></param>
        /// <param name="domainEvents">domainEvents to validate using <paramref name="validators" /></param>
        /// <returns>list of errors for each domainEvent</returns>
        private static IDictionary<DomainEvent, IList<IResult>> Validate(IList<ICommandValidator> validators, IList<DomainEvent> domainEvents)
            => domainEvents.ToDictionary(
                               @event => @event,
                               @event => (IList<IResult>) validators.Select(v => v.ValidateDomainEvent(@event))
                                                                    .ToList())
                           .Where(kvp => kvp.Value.Any(r => r.IsError))
                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
