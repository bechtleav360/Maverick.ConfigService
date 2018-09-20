using System;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    public class SchemaCreated : DomainEvent
    {
        public SchemaCreated(string schemaName, ConfigKeyAction[] data, DateTime when)
        {
            SchemaName = schemaName;
            Data = data;
            When = when;
        }

        public ConfigKeyAction[] Data { get; }

        /// <inheritdoc />
        public override string EventType => nameof(SchemaCreated);

        public string SchemaName { get; }

        public DateTime When { get; }
    }
}