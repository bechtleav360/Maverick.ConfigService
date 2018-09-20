using System;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    public class SchemaCreated : DomainEvent
    {
        public string SchemaName { get; }
        public ConfigKeyAction[] Data { get; }
        public DateTime When { get; }

        [JsonConstructor]
        internal SchemaCreated(string schemaName, ConfigKeyAction[] data, DateTime @when)
        {
            SchemaName = schemaName;
            Data = data;
            When = when;
        }

        /// <inheritdoc />
        public override string EventType => nameof(SchemaCreated);
    }
}