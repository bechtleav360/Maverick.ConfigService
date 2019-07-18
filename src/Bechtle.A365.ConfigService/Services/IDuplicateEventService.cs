using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
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
    ///     component that stores and retrieves current <see cref="HistoryStatus"/> for any given <see cref="DomainEvent"/>
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
        HistoryStatus GetEventStatus(DomainEvent domainEvent);
    }

    /// <summary>
    ///     status of a DomainEvent within the recorded history
    /// </summary>
    public enum HistoryStatus
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
        Recorded
    }

    /// <inheritdoc />
    public class MemoryEventHistoryService : IEventHistoryService
    {
        private readonly ILogger _logger;
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _projectionStore;
        private readonly Dictionary<DomainEvent, HistoryStatus> _eventStatuses;

        /// <inheritdoc />
        public MemoryEventHistoryService(ILogger<MemoryEventHistoryService> logger,
                                         IEventStore eventStore,
                                         IProjectionStore projectionStore)
        {
            _logger = logger;
            _eventStore = eventStore;
            _projectionStore = projectionStore;
            _eventStatuses = new Dictionary<DomainEvent, HistoryStatus>();
        }

        /// <inheritdoc />
        public void AddDomainEventMark(DomainEvent domainEvent) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public void RemoveDomainEventMark(DomainEvent domainEvent) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public HistoryStatus GetEventStatus(DomainEvent domainEvent)
        {
            if (IsEventAlreadyProjected(domainEvent) || IsEventInEventStore(domainEvent))
                return HistoryStatus.Recorded;

            if (IsEventMarked(domainEvent))
                return HistoryStatus.Marked;

            return HistoryStatus.Unknown;
        }

        private bool IsEventAlreadyProjected(DomainEvent domainEvent)
        {
            // @TODO: holy fuck, this seems like a huge task... @future-sven get fucked
            return false;
        }

        private bool IsEventInEventStore(DomainEvent domainEvent)
        {
            var events = _eventStore.ReplayEvents()
                                    .RunSync()
                                    .ToList();

            foreach (var (_, @event) in events)
            {
                if (@event.Equals(domainEvent))
                    return true;
            }

            return false;
        }

        // @TODO: how do we event mark stuff internally?!
        private bool IsEventMarked(DomainEvent domainEvent) => false;
    }
}