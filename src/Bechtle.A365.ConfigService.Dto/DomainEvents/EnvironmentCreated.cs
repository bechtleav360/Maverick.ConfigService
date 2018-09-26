using System;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <summary>
    /// </summary>
    public class EnvironmentCreated : DomainEvent
    {
        public EnvironmentCreated(string environmentName, ConfigKeyAction[] data, DateTime when)
        {
            EnvironmentName = environmentName;
            Data = data;
            When = when;
        }

        public ConfigKeyAction[] Data { get; }
        public string EnvironmentName { get; }

        /// <inheritdoc />
        public override string EventType => nameof(EnvironmentCreated);

        public DateTime When { get; }
    }
}