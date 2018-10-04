using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    /// </summary>
    public abstract class DomainEvent
    {
        /// <summary>
        /// </summary>
        public virtual string EventType => GetEventType(GetType());

        public static string GetEventType<T>() where T : DomainEvent => typeof(T).Name;

        public static string GetEventType(Type domainEventType)
        {
            if (typeof(DomainEvent).IsAssignableFrom(domainEventType))
                return domainEventType.Name;
            throw new NotSupportedException($"type '{domainEventType.FullName}' does not inherit from '{typeof(DomainEvent).FullName}'");
        }
    }
}