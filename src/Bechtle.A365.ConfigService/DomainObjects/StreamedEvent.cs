using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Data used by a <see cref="StreamedObject" /> to update its own Information to the desired State
    /// </summary>
    public class StreamedEvent
    {
        /// <summary>
        ///     Some DomainEvent that may be Evaluated, depending on its Type
        /// </summary>
        public DomainEvent DomainEvent { get; set; }

        /// <summary>
        ///     Utc-Time of when the Event was originally recorded
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        ///     EventStore-Version that this Event represents
        /// </summary>
        public long Version { get; set; }
    }
}