using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Default-Implementation of <see cref="IDomainObjectManager" />
    /// </summary>
    public class DomainObjectManager : IDomainObjectManager
    {
        private readonly IEventStore _eventStore;
        private readonly IEventStoreOptionsProvider _optionsProvider;
        private readonly ProjectionStatusCheck _projectionStatus;
        private readonly EventStoreConnectionConfiguration _eventStoreConfiguration;
        private readonly ILogger<DomainObjectManager> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly IList<ICommandValidator> _validators;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectManager" />
        /// </summary>
        /// <param name="objectStore">instance to store/load DomainObjects to/from</param>
        /// <param name="eventStore">IEventStore to write new domain-events to</param>
        /// <param name="optionsProvider"></param>
        /// <param name="eventStoreConfiguration">eventStore-configuration</param>
        /// <param name="validators">list of domain-event-validators, to use for validating generated Domain-Events</param>
        /// <param name="projectionStatus">status-check that show if we're currently projecting events</param>
        /// <param name="logger"></param>
        public DomainObjectManager(
            IDomainObjectStore objectStore,
            IEventStore eventStore,
            IEventStoreOptionsProvider optionsProvider,
            IOptionsSnapshot<EventStoreConnectionConfiguration> eventStoreConfiguration,
            IEnumerable<ICommandValidator> validators,
            ProjectionStatusCheck projectionStatus,
            ILogger<DomainObjectManager> logger)
        {
            _objectStore = objectStore;
            _eventStore = eventStore;
            _optionsProvider = optionsProvider;
            _projectionStatus = projectionStatus;
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
                new List<DomainEvent> { new EnvironmentLayersModified(environmentIdentifier, layerIdentifiers.ToArray()) },
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CloneLayer(LayerIdentifier sourceIdentifier, LayerIdentifier targetIdentifier, CancellationToken cancellationToken)
            => ModifyObject<EnvironmentLayer, LayerIdentifier>(
                sourceIdentifier,
                new List<DomainEvent> { new EnvironmentLayerCopied(sourceIdentifier, targetIdentifier) },
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateConfiguration(
            ConfigurationIdentifier identifier,
            DateTime? validFrom,
            DateTime? validTo,
            CancellationToken cancellationToken)
            => CreateObject<PreparedConfiguration, ConfigurationIdentifier>(
                identifier,
                new List<DomainEvent> { new ConfigurationBuilt(identifier, validFrom, validTo) },
                true,
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
                        : new EnvironmentCreated(identifier)
                },
                false,
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => CreateObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> { new EnvironmentLayerCreated(identifier) },
                false,
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> CreateStructure(
            StructureIdentifier identifier,
            IDictionary<string, string?> keys,
            IDictionary<string, string?> variables,
            CancellationToken cancellationToken)
            => CreateObject<ConfigStructure, StructureIdentifier>(
                identifier,
                new List<DomainEvent> { new StructureCreated(identifier, keys, variables) },
                false,
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => DeleteObject<ConfigEnvironment, EnvironmentIdentifier>(
                identifier,
                new List<DomainEvent> { new EnvironmentDeleted(identifier) },
                cancellationToken);

        /// <inheritdoc />
        public async Task<IResult> DeleteLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
        {
            // everything from here to 'return await ...' is to prevent deleting layers that are still assigned
            IResult<Page<EnvironmentIdentifier>> environmentResult =
                await ListObjects<ConfigEnvironment, EnvironmentIdentifier>(
                    QueryRange.All,
                    cancellationToken);

            if (environmentResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load environments to check if layer {Layer} is assigned, before deleting it",
                    identifier);
                return environmentResult;
            }

            Page<EnvironmentIdentifier> environments = environmentResult.CheckedData;

            foreach (EnvironmentIdentifier envId in environments.Items)
            {
                IResult<ConfigEnvironment> envResult = await LoadObject<ConfigEnvironment, EnvironmentIdentifier>(
                                                           envId,
                                                           cancellationToken);

                if (envResult.IsError)
                {
                    _logger.LogWarning(
                        "unable to load environment {Environment} to check if "
                        + "layer {Layer} is assigned, before deleting it",
                        envId,
                        identifier);
                    return envResult;
                }

                ConfigEnvironment environment = envResult.CheckedData;

                if (environment.Layers.All(l => l != identifier))
                {
                    continue;
                }

                _logger.LogWarning(
                    "unable to delete layer {Layer}, because it's currently assigned to {Environment}",
                    identifier,
                    envId);
                return Result.Error(
                    $"layer {identifier} is still assigned to {envId}, cannot delete",
                    ErrorCode.LayerStillAssigned);
            }

            // layer isn't assigned - delete it
            return await DeleteObject<EnvironmentLayer, LayerIdentifier>(
                       identifier,
                       new List<DomainEvent> { new EnvironmentLayerDeleted(identifier) },
                       cancellationToken);
        }

        /// <inheritdoc />
        public Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<PreparedConfiguration, ConfigurationIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<PreparedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long version, CancellationToken cancellationToken)
            => LoadObject<PreparedConfiguration, ConfigurationIdentifier>(identifier, version, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(
            EnvironmentIdentifier environment,
            QueryRange range,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(c => c.Environment == environment, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(
            StructureIdentifier structure,
            QueryRange range,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(c => c.Structure == structure, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(
            EnvironmentIdentifier environment,
            QueryRange range,
            long version,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(version, c => c.Environment == environment, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(QueryRange range, long version, CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(version, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<ConfigurationIdentifier>>> GetConfigurations(
            StructureIdentifier structure,
            QueryRange range,
            long version,
            CancellationToken cancellationToken)
            => ListObjects<PreparedConfiguration, ConfigurationIdentifier>(version, c => c.Structure == structure, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigEnvironment, EnvironmentIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<ConfigEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long version, CancellationToken cancellationToken)
            => LoadObject<ConfigEnvironment, EnvironmentIdentifier>(identifier, version, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<EnvironmentIdentifier>>> GetEnvironments(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigEnvironment, EnvironmentIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<EnvironmentIdentifier>>> GetEnvironments(QueryRange range, long version, CancellationToken cancellationToken)
            => ListObjects<ConfigEnvironment, EnvironmentIdentifier>(version, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<EnvironmentLayer>> GetLayer(LayerIdentifier identifier, long version, CancellationToken cancellationToken)
            => LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, version, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<LayerIdentifier>>> GetLayers(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<EnvironmentLayer, LayerIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<LayerIdentifier>>> GetLayers(QueryRange range, long version, CancellationToken cancellationToken)
            => ListObjects<EnvironmentLayer, LayerIdentifier>(version, range, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigurationIdentifier>>> GetStaleConfigurations(QueryRange range, CancellationToken cancellationToken)
        {
            IResult<Page<ConfigurationIdentifier>> configIdResult = await ListObjects<PreparedConfiguration, ConfigurationIdentifier>(
                                                                        QueryRange.All,
                                                                        CancellationToken.None);

            if (configIdResult.IsError)
            {
                _logger.LogWarning(
                    "unable to query stale configs - unable to list all configurations to check their stale-ness: {Code} {Message}",
                    configIdResult.Code,
                    configIdResult.Message);
                return Result.Error<Page<ConfigurationIdentifier>>(configIdResult.Message, configIdResult.Code);
            }

            var staleConfigurationIds = new List<ConfigurationIdentifier>();
            foreach (ConfigurationIdentifier configId in configIdResult.CheckedData.Items)
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
                    return Result.Error<Page<ConfigurationIdentifier>>(configIdResult.Message, configIdResult.Code);
                }

                IDictionary<string, string> metadata = metadataResult.CheckedData;
                bool isConfigStale = metadata.ContainsKey("stale") && JsonConvert.DeserializeObject<bool>(metadata["stale"]);

                if (isConfigStale)
                {
                    staleConfigurationIds.Add(configId);
                }
            }

            // paginate at the end when we have the full list, so the returned ids will be stable across multiple requests
            IList<ConfigurationIdentifier> totalList = staleConfigurationIds.OrderBy(id => id.ToString())
                                                                            .ToList();

            IList<ConfigurationIdentifier> pagedList = totalList.Skip(range.Offset)
                                                                .Take(range.Length)
                                                                .ToList();

            var page = new Page<ConfigurationIdentifier>
            {
                Items = pagedList,
                Count = pagedList.Count,
                Offset = range.Offset,
                TotalCount = totalList.Count
            };

            return Result.Success(page);
        }

        /// <inheritdoc />
        public Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, CancellationToken cancellationToken)
            => LoadObject<ConfigStructure, StructureIdentifier>(identifier, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<ConfigStructure>> GetStructure(StructureIdentifier identifier, long version, CancellationToken cancellationToken)
            => LoadObject<ConfigStructure, StructureIdentifier>(identifier, version, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<StructureIdentifier>>> GetStructures(QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<StructureIdentifier>>> GetStructures(string name, QueryRange range, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(s => s.Name == name, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<StructureIdentifier>>> GetStructures(QueryRange range, long version, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(version, range, cancellationToken);

        /// <inheritdoc />
        public Task<IResult<Page<StructureIdentifier>>> GetStructures(string name, QueryRange range, long version, CancellationToken cancellationToken)
            => ListObjects<ConfigStructure, StructureIdentifier>(version, s => s.Name == name, range, cancellationToken);

        /// <inheritdoc />
        public async Task<IResult> ImportLayer(LayerIdentifier identifier, IList<EnvironmentLayerKey> keys, CancellationToken cancellationToken)
        {
            var events = new List<DomainEvent>();

            IResult<EnvironmentLayer> result = await LoadObject<EnvironmentLayer, LayerIdentifier>(identifier, cancellationToken);
            if (result.IsError)
            {
                if (result.Code == ErrorCode.NotFound)
                {
                    events.Add(new EnvironmentLayerCreated(identifier));
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

            ConfigKeyAction[] actions = keys.Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                            .ToArray();
            events.Add(
                new EnvironmentLayerKeysImported(
                    identifier,
                    actions));

            return await WriteEventsInternal(events);
        }

        /// <inheritdoc />
        public async Task<IResult<bool>> IsStale(ConfigurationIdentifier identifier)
        {
            IResult<IDictionary<string, string>> metadataResult = await _objectStore.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(identifier);

            if (metadataResult.IsError)
            {
                return Result.Error<bool>(metadataResult.Message, metadataResult.Code);
            }

            IDictionary<string, string> metadata = metadataResult.CheckedData;
            if (!metadata.TryGetValue("stale", out string? staleProperty))
            {
                return Result.Success(true);
            }

            var stale = JsonConvert.DeserializeObject<bool>(staleProperty);
            return Result.Success(stale);
        }

        /// <inheritdoc />
        public Task<IResult> ModifyLayerKeys(LayerIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => ModifyObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> { new EnvironmentLayerKeysModified(identifier, actions.ToArray()) },
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> ModifyStructureVariables(StructureIdentifier identifier, IList<ConfigKeyAction> actions, CancellationToken cancellationToken)
            => ModifyObject<ConfigStructure, StructureIdentifier>(
                identifier,
                new List<DomainEvent> { new StructureVariablesModified(identifier, actions.ToArray()) },
                cancellationToken);

        /// <inheritdoc />
        public Task<IResult> UpdateTags(
            LayerIdentifier identifier,
            IEnumerable<string> addedTags,
            IEnumerable<string> removedTags,
            CancellationToken cancellationToken)
            => ModifyObject<EnvironmentLayer, LayerIdentifier>(
                identifier,
                new List<DomainEvent> { new EnvironmentLayerTagsChanged(identifier, addedTags.ToList(), removedTags.ToList()) },
                cancellationToken);

        private async Task<IResult> CreateObject<TObject, TIdentifier>(
            TIdentifier identifier,
            IList<DomainEvent> createEvents,
            bool allowRecreation,
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
            // if we mustn't re-create this object, act as if we did our job.
            // some can be re-created at different times (Configuration)
            if (!allowRecreation && !result.IsError)
            {
                _logger.LogDebug(
                    "skipping creation of {DomainObject} with id {Identifier} because it already exists",
                    typeof(TObject).Name,
                    identifier);
                return Result.Success();
            }

            // thing couldn't be found, but not because it doesn't exist
            // this means there is an actual problem and we can't create the thing
            if (result.Code != ErrorCode.None && result.Code != ErrorCode.NotFound)
            {
                _logger.LogWarning(
                    "unable to create object {DomainObject} with id {Identifier}: {ErrorCode} {Message}",
                    typeof(TObject).Name,
                    identifier,
                    result.Code,
                    result.Message);
                return Result.Error(result.Message, result.Code);
            }

            return await WriteEventsInternal(createEvents);
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

            return await WriteEventsInternal(deleteEvents);
        }

        private async Task<IResult<Page<TIdentifier>>> ListObjects<TObject, TIdentifier>(QueryRange range, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => await ListObjects<TObject, TIdentifier>(_ => true, range, cancellationToken);

        private async Task<IResult<Page<TIdentifier>>> ListObjects<TObject, TIdentifier>(
            Func<TIdentifier, bool> filter,
            QueryRange range,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            cancellationToken.ThrowIfCancellationRequested();

            IResult<Page<TIdentifier>> result = await _objectStore.ListAll<TObject, TIdentifier>(filter, range);

            if (!result.IsError)
            {
                return Result.Success(result.CheckedData);
            }

            _logger.LogWarning(
                "unable to list instances of domain-object '{DomainObject}': {ErrorCode} {Message}",
                typeof(TObject).Name,
                result.Code,
                result.Message);

            return result;
        }

        private async Task<IResult<Page<TIdentifier>>> ListObjects<TObject, TIdentifier>(long version, QueryRange range, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => await ListObjects<TObject, TIdentifier>(version, _ => true, range, cancellationToken);

        private async Task<IResult<Page<TIdentifier>>> ListObjects<TObject, TIdentifier>(
            long version,
            Func<TIdentifier, bool> filter,
            QueryRange range,
            CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            cancellationToken.ThrowIfCancellationRequested();

            IResult<Page<TIdentifier>> result = await _objectStore.ListAll<TObject, TIdentifier>(version, filter, range);

            if (!result.IsError)
            {
                return Result.Success(result.CheckedData);
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
            cancellationToken.ThrowIfCancellationRequested();

            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            IResult<TObject> result = await _objectStore.Load<TObject, TIdentifier>(identifier);

            if (!result.IsError)
            {
                return Result.Success(result.CheckedData);
            }

            _logger.LogWarning(
                "unable to load domain-object '{DomainObject}' with id '{Identifier}': {ErrorCode} {Message}",
                typeof(TObject).Name,
                identifier,
                result.Code,
                result.Message);

            return result;
        }

        private async Task<IResult<TObject>> LoadObject<TObject, TIdentifier>(TIdentifier identifier, long version, CancellationToken cancellationToken)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            IResult<TObject> result = await _objectStore.Load<TObject, TIdentifier>(identifier, version);

            if (!result.IsError)
            {
                return Result.Success(result.CheckedData);
            }

            _logger.LogWarning(
                "unable to load domain-object '{DomainObject}' with id '{Identifier}' at version {Version}: {ErrorCode} {Message}",
                typeof(TObject).Name,
                identifier,
                version,
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

            return await WriteEventsInternal(modificationEvents);
        }

        private ExpectRevision ToRevision(long eventNumber) => eventNumber switch
        {
            var x when x < 0 => ExpectRevision.NotExisting(),
            _ => ExpectRevision.AtPosition(StreamPosition.Revision((ulong)eventNumber))
        };

        /// <summary>
        ///     validate all recorded events with the given <see cref="ICommandValidator" />
        /// </summary>
        /// <param name="validators">validators used to validate <paramref name="domainEvents" /></param>
        /// <param name="domainEvents">domainEvents to validate using <paramref name="validators" /></param>
        /// <returns>list of errors for each domainEvent</returns>
        private static IDictionary<DomainEvent, IList<IResult>> Validate(IList<ICommandValidator> validators, IList<DomainEvent> domainEvents)
            => domainEvents.ToDictionary(
                               @event => @event,
                               @event => (IList<IResult>)validators.Select(v => v.ValidateDomainEvent(@event))
                                                                   .ToList())
                           .Where(kvp => kvp.Value.Any(r => r.IsError))
                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        private async Task<IResult> WriteEventsInternal(IList<DomainEvent> events)
        {
            // if we're currently projecting events, wait a little
            // this is for backwards compatibility -> ideally this would be a user-/client-issue,
            // and be solved using OCC / ETags or something along those lines
            // ---
            // this is technically the wrong thing to do, because the write relies on assumptions that may or may not
            // be valid anymore, because the current projection may change data this write is relying on.
            // we're doing it this way, because of the way this service is being used:
            // - many reads & few writes
            // - used primarily by technical admins
            //   - this should be re-evaluated once subject-admins can change stuff using a streamlined Config-UI / Admin-UI
            // - technical admins only write during setup / maintenance
            DateTime waitUntil = DateTime.Now + TimeSpan.FromSeconds(5);
            while (_projectionStatus.IsCurrentlyProjecting()
                   && DateTime.Now < waitUntil)
            {
                _logger.LogDebug("projection is currently processing events, delaying write-action");
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            // do this as early as possible, so we can detect if changes are made while we get ES-config and split events
            IResult<long> lastProjectedEventResult = await _objectStore.GetProjectedVersion();
            if (lastProjectedEventResult.IsError)
            {
                _logger.LogWarning("unable to determine which event was last projected to write safely into stream");
                return lastProjectedEventResult;
            }

            long lastProjectedEvent = lastProjectedEventResult.Data;

            long maxEventSize;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await _optionsProvider.LoadConfiguration(cts.Token);
                maxEventSize = _optionsProvider.EventSizeLimited ? _optionsProvider.MaxEventSizeInBytes : long.MaxValue;
            }

            // convert *our*-type of DomainEvent to the generic late-binding one
            IResult<IList<IDomainEvent>> splitEvents = ConvertAndSplitEvents(events, maxEventSize);
            if (splitEvents.IsError)
            {
                return splitEvents;
            }

            IList<IDomainEvent> domainEvents = splitEvents.CheckedData;

            // write all events one-by-one, because ES writes all events in a single message
            // ---
            // this is not a ServiceBase.EventStore limitation, but a EventStore.EventStore one
            // ServiceBase.EventStore.WriteEventsAsync is just passing stuff around and calling _.AppendToStreamAsync(,,{events})
            // ---
            // this library is a cluster-fuck
            var offset = 0;
            try
            {
                foreach (IDomainEvent domainEvent in domainEvents)
                {
                    await _eventStore.WriteEventsAsync(
                        new[] { domainEvent },
                        _eventStoreConfiguration.Stream,
                        ToRevision(lastProjectedEvent + offset));
                    KnownMetrics.EventsWritten.WithLabels(domainEvent.Type).Inc();

                    ++offset;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "error while writing {DomainEvents} domainEvents, some events may have been written before this error",
                    domainEvents.Count);
                return Result.Error("unable to write events to EventStore", ErrorCode.DbUpdateError);
            }

            return Result.Success();
        }

        private IResult<IList<IDomainEvent>> ConvertAndSplitEvents(IList<DomainEvent> events, long maxEventSize)
        {
            var options = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            IList<IDomainEvent> domainEvents = new List<IDomainEvent>();

            try
            {
                // this is *the* most resource-heavy way i could think of, but it's the easiest way of knowing if
                // the written events will fit into the size-constraints of ES
                var eventStack = new Stack<DomainEvent>(events.Reverse());
                while (eventStack.TryPop(out DomainEvent? item))
                {
                    var domainEvent = new LateBindingDomainEvent<DomainEvent>("Anonymous", item);

                    // 1048576
                    int jsonLength = JsonConvert.SerializeObject(domainEvent, options).Length;

                    if (jsonLength < maxEventSize)
                    {
                        domainEvents.Add(domainEvent);
                        continue;
                    }

                    IList<DomainEvent> smallerEvents = item.Split().Reverse().ToList();
                    if (smallerEvents.Count == 1)
                    {
                        return Result.Error<IList<IDomainEvent>>(
                            "domainEvents exceed maximum size supported by EventStore, and cannot be resized",
                            ErrorCode.PrerequisiteFailed);
                    }

                    foreach (DomainEvent smallerItem in smallerEvents)
                    {
                        eventStack.Push(smallerItem);
                    }
                }
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "unable to approximate size of DomainEvents to be written");
                return Result.Error<IList<IDomainEvent>>("unable to approximate size of DomainEvents", ErrorCode.PrerequisiteFailed);
            }

            return Result.Success(domainEvents);
        }
    }
}
