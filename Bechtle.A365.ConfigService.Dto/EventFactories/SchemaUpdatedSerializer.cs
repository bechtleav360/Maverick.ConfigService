using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public class SchemaUpdatedSerializer : IDomainEventSerializer<SchemaUpdated>
    {
        /// <inheritdoc />
        DomainEvent IDomainEventSerializer.Deserialize(byte[] data, byte[] metadata) => Deserialize(data, metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent created) => Serialize(created as SchemaUpdated);

        /// <inheritdoc />
        public SchemaUpdated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<SchemaUpdated>(json);
        }

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(SchemaUpdated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );
    }
}