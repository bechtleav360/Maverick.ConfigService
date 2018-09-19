using System.Text;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.DomainEvents
{
    public class DummyEvent : DomainEvent
    {
        /// <inheritdoc />
        public DummyEvent()
        {
        }

        /// <inheritdoc />
        public DummyEvent(RecordedEvent recordedEvent) : base(recordedEvent)
        {
        }

        /// <inheritdoc />
        public override string EventType => "Dummy Event Type";

        /// <inheritdoc />
        public override byte[] Serialize() => Encoding.UTF8.GetBytes("Dummy Event Data");

        /// <inheritdoc />
        public override byte[] SerializeMetadata() => Encoding.UTF8.GetBytes("Dummy Event Metadata");
    }
}