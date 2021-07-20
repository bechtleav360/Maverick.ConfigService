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
        ///     Get the DomainEvent-Type for the given Type
        /// </summary>
        /// <typeparam name="T">Sub-Type of <see cref="DomainEvent"/></typeparam>
        /// <returns>type-identifier for the given Type</returns>
        public static string GetEventType<T>() where T : DomainEvent => typeof(T).Name;

        /// <summary>
        ///     Get the DomainEvent-Type for the given Type
        /// </summary>
        /// <param name="domainEventType">Sub-Type of <see cref="DomainEvent"/></param>
        /// <returns>type-identifier for the given Type</returns>
        /// <exception cref="NotSupportedException">
        ///     thrown when <paramref name="domainEventType"/> is not assignable to <see cref="DomainEvent"/>
        /// </exception>
        public static string GetEventType(Type domainEventType)
        {
            if (typeof(DomainEvent).IsAssignableFrom(domainEventType))
            {
                return domainEventType.Name;
            }

            throw new NotSupportedException($"type '{domainEventType.FullName}' does not inherit from '{typeof(DomainEvent).FullName}'");
        }

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
    }
}
