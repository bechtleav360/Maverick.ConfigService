using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     internal EventStore interface
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        ///     event triggered once a new Event has been received from EventStore
        /// </summary>
        event EventHandler<(EventStoreSubscription Subscription, ResolvedEvent ResolvedEvent)> EventAppeared;

        /// <summary>
        ///     get the EventNumber of the newest Event
        /// </summary>
        /// <returns></returns>
        Task<long> GetCurrentEventNumber();

        /// <summary>
        ///     read the Event-History as a stream and execute an action for each event,
        ///     filtering out events that do not match <paramref name="streamFilter" />
        /// </summary>
        /// <param name="streamFilter"></param>
        /// <param name="streamProcessor"></param>
        /// <param name="readSize"></param>
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<(RecordedEvent RecordedEvent, DomainEventMetadata Metadata), bool> streamFilter,
                                  Func<(RecordedEvent RecordedEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                  int readSize = 64,
                                  StreamDirection direction = StreamDirection.Forwards,
                                  long startIndex = -1);

        /// <summary>
        ///     read the Event-History as a stream and execute an action for each event
        /// </summary>
        /// <param name="streamProcessor">action executed for each event until it returns false</param>
        /// <param name="readSize">number of events read from stream in one go. can't be greater than 4096</param>
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<(RecordedEvent RecordedEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                  int readSize = 64,
                                  StreamDirection direction = StreamDirection.Forwards,
                                  long startIndex = -1);

        /// <summary>
        ///     write multiple DomainEvents into the store
        /// </summary>
        /// <param name="domainEvents"></param>
        /// <returns></returns>
        Task<long> WriteEvents(IList<DomainEvent> domainEvents);
    }
}