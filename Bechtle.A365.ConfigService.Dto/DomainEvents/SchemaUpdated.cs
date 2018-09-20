using System;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    public class SchemaUpdated : DomainEvent
    {
        public SchemaUpdated(string schemaName, ConfigKeyAction[] data, DateTime when)
        {
            SchemaName = schemaName;
            Data = data;
            When = when;
        }

        public ConfigKeyAction[] Data { get; }

        /// <inheritdoc />
        public override string EventType => nameof(SchemaUpdated);

        public string SchemaName { get; }

        public DateTime When { get; }
    }
}