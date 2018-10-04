namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Configuration has been built with information from <see cref="Environment" /> and <see cref="Structure" />
    /// </summary>
    public class ConfigurationBuilt : DomainEvent
    {
        /// <inheritdoc />
        public ConfigurationBuilt(EnvironmentIdentifier environment, StructureIdentifier structure)
        {
            Environment = environment;
            Structure = structure;
        }

        /// <inheritdoc />
        public ConfigurationBuilt()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Environment { get; set; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Structure { get; set; }
    }
}