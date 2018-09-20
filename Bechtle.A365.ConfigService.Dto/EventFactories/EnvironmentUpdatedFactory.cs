using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.EventFactories
{
    public static class EnvironmentUpdatedFactory
    {
        public static EnvironmentUpdated Build(string environmentName, IEnumerable<ConfigKeyAction> data, DateTime when)
            => new EnvironmentUpdated(environmentName, data.ToArray(), when);

        public static (byte[] Data, byte[] Metadata) Serialize(EnvironmentUpdated created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );

        public static EnvironmentUpdated Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<EnvironmentUpdated>(json);
        }

        public static EnvironmentUpdated Deserialize((byte[] Data, byte[] Metadata) tuple) => Deserialize(tuple.Data, tuple.Metadata);
    }
}