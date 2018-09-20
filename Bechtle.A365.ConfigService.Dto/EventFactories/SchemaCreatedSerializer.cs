using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public class SchemaCreatedSerializer : IDomainEventSerializer<SchemaCreated>
    {
        /// <inheritdoc />
        DomainEvent IDomainEventSerializer.Deserialize(byte[] data, byte[] metadata) => Deserialize(data, metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent created) => Serialize(created as SchemaCreated);

        /// <inheritdoc />
        public SchemaCreated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<SchemaCreated>(json);
        }

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(SchemaCreated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );
    }
}