using System;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <summary>
    /// </summary>
    public class EnvironmentUpdated : DomainEvent
    {
        public string EnvironmentName { get; }
        public ConfigKeyAction[] Data { get; }
        public DateTime When { get; }

        [JsonConstructor]
        internal EnvironmentUpdated(string environmentName, ConfigKeyAction[] data, DateTime @when)
        {
            EnvironmentName = environmentName;
            Data = data;
            When = when;
        }

        /// <inheritdoc />
        public override string EventType => nameof(EnvironmentUpdated);
    }
}