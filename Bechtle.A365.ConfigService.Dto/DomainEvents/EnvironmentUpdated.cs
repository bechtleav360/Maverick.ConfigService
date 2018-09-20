using System;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    public class EnvironmentUpdated : DomainEvent
    {
        public EnvironmentUpdated(string environmentName, ConfigKeyAction[] data, DateTime when)
        {
            EnvironmentName = environmentName;
            Data = data;
            When = when;
        }

        public ConfigKeyAction[] Data { get; }
        public string EnvironmentName { get; }

        /// <inheritdoc />
        public override string EventType => nameof(EnvironmentUpdated);

        public DateTime When { get; }
    }
}