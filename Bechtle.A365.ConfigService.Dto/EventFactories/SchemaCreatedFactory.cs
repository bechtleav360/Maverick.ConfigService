using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public static class SchemaCreatedFactory
    {
        public static SchemaCreated Build(string schemaName, IEnumerable<ConfigKeyAction> data, DateTime when)
            => new SchemaCreated(schemaName, data.ToArray(), when);

        public static (byte[] Data, byte[] Metadata) Serialize(SchemaCreated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );

        public static SchemaCreated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<SchemaCreated>(json);
        }

        public static SchemaCreated Deserialize((byte[] Data, byte[] Metadata) tuple) => Deserialize(tuple.Data, tuple.Metadata);
    }
}