using System;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.DomainEvents
{
    public abstract class DomainEvent
    {
        /// <summary>
        /// </summary>
        public DomainEvent()
        {
        }

        /// <summary>
        ///     use this when de-serializing event from <see cref="RecordedEvent"/>
        /// </summary>
        /// <param name="recordedEvent"></param>
        public DomainEvent(RecordedEvent recordedEvent)
        {
        }

        public abstract string EventType { get; }

        public abstract byte[] Serialize();

        public abstract byte[] SerializeMetadata();

        public static DomainEvent From(RecordedEvent recordedEvent)
        {
            switch (recordedEvent.EventType)
            {
                case "Dummy Event Type":
                    return new DummyEvent(recordedEvent);

                default:
                    throw new ArgumentOutOfRangeException($"could not resolve event of type '{recordedEvent.EventType}'");
            }
        }
    }
}