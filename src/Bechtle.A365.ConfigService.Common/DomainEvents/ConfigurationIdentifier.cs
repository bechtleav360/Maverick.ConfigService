using Bechtle.A365.ConfigService.Common.DbObjects;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify a Configuration built from an Environment and a Structure
    /// </summary>
    public class ConfigurationIdentifier : Identifier
    {
        public ConfigurationIdentifier(ProjectedConfiguration projectedConfiguration)
            : this(new EnvironmentIdentifier(projectedConfiguration.ConfigEnvironment),
                   new StructureIdentifier(projectedConfiguration.Structure))
        {
        }

        /// <inheritdoc />
        public ConfigurationIdentifier(EnvironmentIdentifier environment, StructureIdentifier structure)
        {
            Environment = environment;
            Structure = structure;
        }

        /// <inheritdoc />
        public ConfigurationIdentifier()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Environment { get; set; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Structure { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(Environment)}: {Environment}; {nameof(Structure)}: {Structure}]";
    }
}