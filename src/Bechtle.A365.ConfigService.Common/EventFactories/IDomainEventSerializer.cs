using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.EventFactories
{
    public interface IDomainEventSerializer
    {
        DomainEvent Deserialize(byte[] data, byte[] metadata);
        (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent);
    }

    public interface IDomainEventSerializer<T> : IDomainEventSerializer where T : DomainEvent
    {
        new T Deserialize(byte[] data, byte[] metadata);
        (byte[] Data, byte[] Metadata) Serialize(T domainEvent);
    }
}