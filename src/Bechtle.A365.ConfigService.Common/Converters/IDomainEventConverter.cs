using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public interface IDomainEventConverter
    {
        DomainEvent Deserialize(byte[] data, byte[] metadata);
        (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent);
    }

    public interface IDomainEventConverter<T> : IDomainEventConverter where T : DomainEvent
    {
        new T Deserialize(byte[] data, byte[] metadata);
        (byte[] Data, byte[] Metadata) Serialize(T domainEvent);
    }
}