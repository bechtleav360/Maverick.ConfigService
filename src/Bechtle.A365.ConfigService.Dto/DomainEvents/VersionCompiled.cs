using System;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    public class VersionCompiled : DomainEvent
    {
        public VersionCompiled(string environmentName, string schemaName, DateTime when)
        {
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            When = when;
        }

        public string EnvironmentName { get; }

        /// <inheritdoc />
        public override string EventType => nameof(VersionCompiled);

        public string SchemaName { get; }

        public DateTime When { get; }
    }
}