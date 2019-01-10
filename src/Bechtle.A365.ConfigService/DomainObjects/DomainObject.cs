using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     represents an Object of our Business-Domain
    /// </summary>
    public abstract class DomainObject
    {
        /// <summary>
        /// </summary>
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
    }
}