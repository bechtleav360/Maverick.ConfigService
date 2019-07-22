using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     represents an Object of our Business-Domain
    /// </summary>
    public abstract class DomainObject
    {
        /// <inheritdoc />
        public DomainObject()
        {
            RecordedEvents = new List<DomainEvent>();
        }

        /// <summary>
        ///     Events that lead to the DomainObject having the current state (Created, Modified, Deleted)
        /// </summary>
        protected IList<DomainEvent> RecordedEvents { get; }

        /// <summary>
        ///     Save all events that bring the this DomainObject to the desired State
        /// </summary>
        /// <param name="store"></param>
        /// <param name="eventHistory"></param>
        /// <param name="logger"></param>
        public virtual async Task Save(IEventStore store, IEventHistoryService eventHistory, ILogger logger)
        {
            foreach (var @event in RecordedEvents)
            {
                logger?.LogDebug($"checking status for DomainEvent '{@event.EventType}'");

                var status = await eventHistory.GetEventStatus(@event);

                logger?.LogDebug($"status for DomainEvent '{@event.EventType}' = {status:G} / {status:D}");

                switch (status)
                {
                    case EventStatus.Unknown:
                        logger?.LogInformation($"saving DomainEvent '{@event.EventType}'");
                        await store.WriteEvent(@event);
                        break;

                    case EventStatus.Recorded:
                        logger?.LogInformation($"pretending to save DomainEvent '{@event.EventType}', " +
                                               "but same event has already been recorded without being projected");
                        break;

                    case EventStatus.Projected:
                        logger?.LogInformation($"pretending to save DomainEvent '{@event.EventType}', " +
                                               "but same event has already been projected without being superseded");
                        break;

                    case EventStatus.Superseded:
                        logger?.LogInformation($"saving DomainEvent '{@event.EventType}', it's projected but already superseded");
                        await store.WriteEvent(@event);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        ///     Save all events that bring the this DomainObject to the Desired State
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        [Obsolete("this function does not check the DomainEvents for redundancy before writing them, " +
                  "use Save(IEventStore, IEventHistoryService, ILogger) instead", false)]
        public virtual async Task Save(IEventStore store)
        {
            foreach (var @event in RecordedEvents)
                await store.WriteEvent(@event);
        }

        /// <summary>
        ///     validate all recorded events with the given <see cref="ICommandValidator"/>
        /// </summary>
        /// <param name="validators"></param>
        /// <returns></returns>
        public IDictionary<DomainEvent, IList<IResult>> Validate(IList<ICommandValidator> validators)
            => RecordedEvents.ToDictionary(@event => @event,
                                           @event => (IList<IResult>) validators.Select(v => v.ValidateDomainEvent(@event))
                                                                                .ToList())
                             .Where(kvp => kvp.Value.Any(r => r.IsError))
                             .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}