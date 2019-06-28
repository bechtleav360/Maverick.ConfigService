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
                   new StructureIdentifier(projectedConfiguration.Structure),
                   projectedConfiguration.Version)
        {
        }

        /// <inheritdoc />
        public ConfigurationIdentifier(EnvironmentIdentifier environment, StructureIdentifier structure)
            : this(environment, structure, default)
        {
        }

        /// <inheritdoc />
        public ConfigurationIdentifier(EnvironmentIdentifier environment, StructureIdentifier structure, long version)
        {
            Environment = environment;
            Structure = structure;
            Version = version;
        }

        /// <inheritdoc />
        public ConfigurationIdentifier()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Environment { get; set; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Structure { get; set; }

        /// <summary>
        ///     Optional version of this Configuration
        /// </summary>
        public long Version { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(Environment)}: {Environment}; {nameof(Structure)}: {Structure}]";
    }
}