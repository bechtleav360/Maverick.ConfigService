using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;

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
                                                                                .ToList());
    }
}