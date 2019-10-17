using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class DomainEventConverter<T> : IDomainEventConverter<T> where T : DomainEvent
    {
        /// <inheritdoc />
        DomainEvent IDomainEventConverter.Deserialize(byte[] data, byte[] metadata) => Deserialize(data, metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent created) => Serialize(created as T);

        /// <inheritdoc />
        public T Deserialize(byte[] data, byte[] metadata) => JsonSerializer.Deserialize<T>(data);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(T created)
            => (
                   JsonSerializer.SerializeToUtf8Bytes(created),
                   new byte[0]
               );
    }
}