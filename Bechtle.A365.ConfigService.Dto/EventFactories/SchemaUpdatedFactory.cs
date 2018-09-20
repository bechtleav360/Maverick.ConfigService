using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public static class SchemaUpdatedFactory
    {
        public static SchemaUpdated Build(string schemaName, IEnumerable<ConfigKeyAction> data, DateTime when)
            => new SchemaUpdated(schemaName, data.ToArray(), when);

        public static (byte[] Data, byte[] Metadata) Serialize(SchemaUpdated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );

        public static SchemaUpdated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<SchemaUpdated>(json);
        }

        public static SchemaUpdated Deserialize((byte[] Data, byte[] Metadata) tuple) => Deserialize(tuple.Data, tuple.Metadata);
    }
}