using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    // @TODO: add method/s that let the user validate a DomainEvent and check if it's already in the DB
    //        additionally, allow user to see when the original event was written?
    //        make it possible to still send a 'successful' HttpStatusCode when duplicates have been located
    //        Config-Client relies on getting a 20x response when uploading / building its own stuff
    //        duplicates should appear in the logs as warnings!

    // @TODO: add method/s that let user mark the writing of a DomainEvent for future reference (marker)
    //        this marker should be in non-volatile storage (configured db? - new migration for new table)

    /// <summary>
    ///     component that stores and retrieves current <see cref="EventStatus"/> for any given <see cref="DomainEvent"/>
    /// </summary>
    public interface IEventHistoryService
    {
        /// <summary>
        ///     mark the given domainEvent as written to the EventStore
        /// </summary>
        /// <param name="domainEvent"></param>
        void AddDomainEventMark(DomainEvent domainEvent);

        /// <summary>
        ///     mark the given domainEvent as stored in history
        /// </summary>
        /// <param name="domainEvent"></param>
        void RemoveDomainEventMark(DomainEvent domainEvent);

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
        ///     event has been marked to be recorded in history
        /// </summary>
        Marked,

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
        private readonly Dictionary<DomainEvent, EventStatus> _eventStatuses;

        /// <inheritdoc />
        public MemoryEventHistoryService(ILogger<MemoryEventHistoryService> logger,
                                         IEventStore eventStore,
                                         IProjectionStore projectionStore)
        {
            _logger = logger;
            _eventStore = eventStore;
            _projectionStore = projectionStore;
            _eventStatuses = new Dictionary<DomainEvent, EventStatus>();
        }

        /// <inheritdoc />
        public void AddDomainEventMark(DomainEvent domainEvent) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public void RemoveDomainEventMark(DomainEvent domainEvent) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public async Task<EventStatus> GetEventStatus(DomainEvent domainEvent)
        {
            var status = EventStatus.Unknown;

            if (await IsEventMarked(domainEvent))
                status = EventStatus.Marked;

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
                return false;

            // filter out those that don't have the same Type, they can't be what we search
            var projectedEvents = metadataResult.Data
                                                .Where(p => p.Type == domainEvent.EventType)
                                                .ToDictionary(p => p.Index, p => (DomainEvent) null);

            // stream the actual events from EventStore to get the underlying DomainEvents from them
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, @event) = tuple;

                // update projectedEvents with the actual DomainEvents
                if (projectedEvents.ContainsKey(recordedEvent.EventNumber))
                    projectedEvents[recordedEvent.EventNumber] = @event;

                // if all values have been filled we can stop processing more events
                return projectedEvents.Values.All(v => v != null);
            });

            // if any value in projectedEvents is similar to the given domainEvent,
            // the given domainEvent has been projected
            // if none match, we can be reasonably sure it hasn't been projected to DB
            return projectedEvents.Values.Any(e => e.Equals(domainEvent));
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

            return result;
        }

        private async Task<bool> IsEventSuperseded(DomainEvent domainEvent) => false;

        // @TODO: how do we event mark stuff internally?!
        private async Task<bool> IsEventMarked(DomainEvent domainEvent) => false;
    }
}