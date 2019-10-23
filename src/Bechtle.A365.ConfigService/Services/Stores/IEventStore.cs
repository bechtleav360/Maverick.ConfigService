using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     internal EventStore interface
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        ///     replay all events and get the written DomainEvents
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<(RecordedEvent, DomainEvent)>> ReplayEvents(StreamDirection direction = StreamDirection.Forwards);

        /// <summary>
        ///     read the Event-History as a stream and execute an action for each event,
        ///     filtering out events that do not match <paramref name="streamFilter" />
        /// </summary>
        /// <param name="streamFilter"></param>
        /// <param name="streamProcessor"></param>
        /// <param name="readSize"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<RecordedEvent, bool> streamFilter,
                                  Func<(RecordedEvent, DomainEvent), bool> streamProcessor,
                                  int readSize = 64,
                                  StreamDirection direction = StreamDirection.Forwards);

        /// <summary>
        ///     read the Event-History as a stream and execute an action for each event
        /// </summary>
        /// <param name="streamProcessor">action executed for each event until it returns false</param>
        /// <param name="readSize">number of events read from stream in one go. can't be greater than 4096</param>
        /// <param name="direction"></param>
        /// <returns></returns>
        Task ReplayEventsAsStream(Func<(RecordedEvent, DomainEvent), bool> streamProcessor,
                                  int readSize = 64,
                                  StreamDirection direction = StreamDirection.Forwards);

        /// <summary>
        ///     write Event <typeparamref name="T" /> into the store
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task WriteEvent<T>(T domainEvent) where T : DomainEvent;

        /// <summary>
        ///     write multiple DomainEvents into the store
        /// </summary>
        /// <param name="domainEvents"></param>
        /// <returns></returns>
        Task WriteEvents(IList<DomainEvent> domainEvents);
    }
}