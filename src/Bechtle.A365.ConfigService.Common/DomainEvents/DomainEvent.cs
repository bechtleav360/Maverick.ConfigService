using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    /// </summary>
    public abstract class DomainEvent
    {
        /// <summary>
        ///     Get the public Identifier for this type of Event
        /// </summary>
        public virtual string EventType => GetEventType(GetType());

        /// <summary>
        ///     Check if DomainEvent strictly equals another event
        /// </summary>
        /// <param name="other">other instance of DomainEvent to test for equality</param>
        /// <param name="strict">perform test in the strictest manner possible - settings this to false will skip some tests</param>
        /// <returns>true if both DomainEvents represent the same event</returns>
        public abstract bool Equals(DomainEvent other, bool strict);

        /// <summary>
        ///     Get the public Metadata for this DomainEvent
        /// </summary>
        /// <returns></returns>
        public abstract DomainEventMetadata GetMetadata();

        /// <summary>
        ///     Split this DomainEvent into multiple smaller DomainEvents, if possible
        /// </summary>
        /// <returns>list of smaller DomainEvents, or list with this DomainEvent if it can't be split again</returns>
        public virtual IList<DomainEvent> Split() => new List<DomainEvent> {this};

        public static string GetEventType<T>() where T : DomainEvent => typeof(T).Name;

        public static string GetEventType(Type domainEventType)
        {
            if (typeof(DomainEvent).IsAssignableFrom(domainEventType))
                return domainEventType.Name;
            throw new NotSupportedException($"type '{domainEventType.FullName}' does not inherit from '{typeof(DomainEvent).FullName}'");
        }
    }
}