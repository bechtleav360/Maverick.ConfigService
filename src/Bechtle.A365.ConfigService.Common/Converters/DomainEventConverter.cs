﻿using System.Text;
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
        public T Deserialize(byte[] data, byte[] metadata)
        {
            var json = Encoding.UTF8.GetString(data);

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <inheritdoc />
        public (byte[] Data, byte[] Metadata) Serialize(T created)
            => (
                   Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(created)),
                   new byte[0]
               );
    }
}