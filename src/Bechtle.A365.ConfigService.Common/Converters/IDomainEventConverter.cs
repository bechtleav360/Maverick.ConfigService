using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public interface IDomainEventConverter
    {
        DomainEvent DeserializeInstance(ReadOnlyMemory<byte> data);

        (ReadOnlyMemory<byte> Data, ReadOnlyMemory<byte> Metadata) Serialize(DomainEvent domainEvent);

        DomainEventMetadata DeserializeMetadata(ReadOnlyMemory<byte> metadata);
    }

    public interface IDomainEventConverter<T> : IDomainEventConverter where T : DomainEvent
    {
        new T DeserializeInstance(ReadOnlyMemory<byte> data);

        (ReadOnlyMemory<byte> Data, ReadOnlyMemory<byte> Metadata) Serialize(T domainEvent);
    }
}