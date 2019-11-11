using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public interface IDomainEventConverter
    {
        DomainEvent DeserializeInstance(byte[] data);

        (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent);

        DomainEventMetadata DeserializeMetadata(byte[] metadata);
    }

    public interface IDomainEventConverter<T> : IDomainEventConverter where T : DomainEvent
    {
        new T DeserializeInstance(byte[] data);

        (byte[] Data, byte[] Metadata) Serialize(T domainEvent);
    }
}