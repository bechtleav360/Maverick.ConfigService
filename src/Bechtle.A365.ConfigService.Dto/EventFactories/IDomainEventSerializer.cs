using Bechtle.A365.ConfigService.Dto.DomainEvents;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public interface IDomainEventSerializer
    {
        (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent);

        DomainEvent Deserialize(byte[] data, byte[] metadata);
    }

    public interface IDomainEventSerializer<T> : IDomainEventSerializer where T : DomainEvent
    {
        (byte[] Data, byte[] Metadata) Serialize(T domainEvent);

        new T Deserialize(byte[] data, byte[] metadata);
    }
}