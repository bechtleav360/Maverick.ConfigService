using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     component that stores and retrieves current <see cref="EventStatus"/> for any given <see cref="DomainEvent"/>
    /// </summary>
    public interface IEventHistoryService
    {
        /// <summary>
        ///     get the status for a given DomainEvent.
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task<EventStatus> GetEventStatus(DomainEvent domainEvent);
    }

    /// <summary>
    ///     status of a DomainEvent within the recorded history
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        ///     event has not been recorded in history
        /// </summary>
        Unknown,

        /// <summary>
        ///     event has been recorded in history
        /// </summary>
        Recorded,

        /// <summary>
        ///     event has been recorded and projected
        /// </summary>
        Projected,

        /// <summary>
        ///     event has been recorded and projected, but newer events overwrite this action
        /// </summary>
        Superseded,
    }

    /// <inheritdoc />
    public class MemoryEventHistoryService : IEventHistoryService
    {
        private readonly ILogger _logger;
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _projectionStore;

        /// <inheritdoc />
        public MemoryEventHistoryService(ILogger<MemoryEventHistoryService> logger,
                                         IEventStore eventStore,
                                         IProjectionStore projectionStore)
        {
            _logger = logger;
            _eventStore = eventStore;
            _projectionStore = projectionStore;
        }

        /// <inheritdoc />
        public async Task<EventStatus> GetEventStatus(DomainEvent domainEvent)
        {
            var status = EventStatus.Unknown;

            if (await IsEventInEventStore(domainEvent))
                status = EventStatus.Recorded;

            if (await IsEventAlreadyProjected(domainEvent))
                status = EventStatus.Projected;

            if (await IsEventSuperseded(domainEvent))
                status = EventStatus.Superseded;

            return status;
        }

        private async Task<bool> IsEventAlreadyProjected(DomainEvent domainEvent)
        {
            // get the list of events that have already been projected to DB
            var metadataResult = await _projectionStore.Metadata.GetProjectedEventMetadata();
            if (metadataResult.IsError)
            {
                _logger.LogWarning($"could not retrieve metadata for event of type '{domainEvent.EventType}'");
                return false;
            }

            _logger.LogDebug($"retrieved '{metadataResult.Data.Count}' metadata-records for projected events");

            // filter out those that don't have the same Type, they can't be what we search
            var projectedEvents = metadataResult.Data
                                                .Where(p => p.Type == domainEvent.EventType)
                                                .ToDictionary(p => p.Index, p => (DomainEvent) null);

            _logger.LogDebug($"filtered out '{metadataResult.Data.Count - projectedEvents.Count}' metadata-records, " +
                             $"'{projectedEvents.Count}' items left");

            _logger.LogDebug("streaming events to retrieve data for DomainEvents");
            // stream the actual events from EventStore to get the underlying DomainEvents from them
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, @event) = tuple;

                // update projectedEvents with the actual DomainEvents
                if (projectedEvents.ContainsKey(recordedEvent.EventNumber))
                    projectedEvents[recordedEvent.EventNumber] = @event;

                // if all values have been filled we can stop processing more events
                return projectedEvents.Values.Any(v => v == null);
            });

            // if any value in projectedEvents is similar to the given domainEvent,
            // the given domainEvent has been projected
            // if none match, we can be reasonably sure it hasn't been projected to DB
            var entry = projectedEvents.Where(kvp => kvp.Value.Equals(domainEvent))
                                       .Select(kvp => (Index: kvp.Key, Event: kvp.Value))
                                       .FirstOrDefault();

            // check if event is null in the tuple, because tuple is struct and never null
            var result = !(entry.Event is null);

            _logger.LogInformation(result
                                       ? $"DomainEvent '{domainEvent.EventType}' has been projected at ({entry.Index})"
                                       : $"DomainEvent '{domainEvent.EventType}' with same data could not be found in ES-Stream");

            return result;
        }

        private async Task<bool> IsEventInEventStore(DomainEvent domainEvent)
        {
            var result = false;

            // stream all events in EventStore and compare their payload-DomainEvent with what we're given
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (_, @event) = tuple;

                if (@event.Equals(domainEvent))
                {
                    // set status and break further stream processing by returning false
                    result = true;
                    return false;
                }

                // continue stream-processing
                return true;
            });

            _logger.LogDebug(result
                                 ? $"DomainEvent '{domainEvent.EventType}' with same data has been found in ES-Stream"
                                 : $"DomainEvent '{domainEvent.EventType}' with same data could not be found in ES-Stream");

            return result;
        }

        private Task<bool> IsEventSuperseded(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                // events that are unique once written
                case StructureCreated _:
                case EnvironmentCreated _:
                case DefaultEnvironmentCreated _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can't be superseded (fixed)");
                    return Task.FromResult(false);

                // @TODO: see if the modifications of domainEvent have been overwritten, which would cause it to be Superseded
                // events that appear routinely and
                // even though it's already in EventStore it can be written again
                case ConfigurationBuilt _:
                case EnvironmentKeysImported _:
                case EnvironmentKeysModified _:
                case StructureVariablesModified _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can be superseded");
                    return Task.FromResult(true);

                // not implemented
                case StructureDeleted _:
                case EnvironmentDeleted _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can be superseded (fixed)");
                    return Task.FromResult(true);
            }

            return Task.FromResult(true);
        }
    }
}