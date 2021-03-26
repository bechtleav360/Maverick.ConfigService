using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     internal EventStore interface
    /// </summary>
    public interface IEventStore : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///     event triggered once a new Event has been received from EventStore
        /// </summary>
        event EventHandler<(StoreSubscription Subscription, StoredEvent StoredEvent)> EventAppeared;

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
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool> streamFilter,
                                  Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                  StreamDirection direction = StreamDirection.Forwards,
                                  long startIndex = -1);

        /// <summary>
        ///     read the Event-History as a stream and execute an action for each event
        /// </summary>
        /// <param name="streamProcessor">action executed for each event until it returns false</param>
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
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