using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public class EnvironmentUpdatedSerializer : IDomainEventSerializer<EnvironmentUpdated>
    {
        /// <inheritdoc />
        DomainEvent IDomainEventSerializer.Deserialize(byte[] data, byte[] metadata) => Deserialize(data, metadata);

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(DomainEvent created) => Serialize(created as EnvironmentUpdated);

        /// <inheritdoc />
        public EnvironmentUpdated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<EnvironmentUpdated>(json);
        }

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(EnvironmentUpdated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );
    }
}