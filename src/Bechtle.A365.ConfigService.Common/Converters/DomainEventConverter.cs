using System.Text;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Common.Converters
{
    public class DomainEventConverter<T> : IDomainEventConverter<T> where T : DomainEvent
    {
        /// <inheritdoc />
        DomainEvent IDomainEventConverter.DeserializeInstance(byte[] data) => DeserializeInstance(data);

        /// <inheritdoc />
        public T DeserializeInstance(byte[] data) => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));

        /// <inheritdoc />
        public DomainEventMetadata DeserializeMetadata(byte[] metadata) => JsonSerializer.Deserialize<DomainEventMetadata>(metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent) => Serialize(domainEvent as T);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(T domainEvent)
            => (
                   JsonSerializer.SerializeToUtf8Bytes(domainEvent),
                   JsonSerializer.SerializeToUtf8Bytes(domainEvent.GetMetadata())
               );
    }
}