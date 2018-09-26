using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public class VersionCompiledSerializer : IDomainEventSerializer<VersionCompiled>
    {
        /// <inheritdoc />
        DomainEvent IDomainEventSerializer.Deserialize(byte[] data, byte[] metadata) => Deserialize(data, metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent domainEvent) => Serialize(domainEvent as VersionCompiled);

        /// <inheritdoc />
        public VersionCompiled Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<VersionCompiled>(json);
        }

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(VersionCompiled created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );
    }
}