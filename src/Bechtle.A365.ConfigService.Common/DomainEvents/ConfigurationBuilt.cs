namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Configuration has been built with information from <see cref="EnvironmentIdentifier" /> and <see cref="StructureIdentifier" />
    /// </summary>
    public class ConfigurationBuilt : DomainEvent
    {
        /// <inheritdoc />
        public ConfigurationBuilt(EnvironmentIdentifier environment, StructureIdentifier structure)
        {
            Identifier = new ConfigurationIdentifier(environment, structure);
        }

        /// <inheritdoc />
        public ConfigurationBuilt()
        {
        }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public ConfigurationIdentifier Identifier { get; set; }
    }
}