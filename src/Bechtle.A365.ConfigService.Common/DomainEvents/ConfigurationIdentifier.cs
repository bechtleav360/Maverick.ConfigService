using System;
using Bechtle.A365.ConfigService.Common.DbObjects;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify a Configuration built from an Environment and a Structure
    /// </summary>
    public class ConfigurationIdentifier : Identifier, IEquatable<ConfigurationIdentifier>
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

        public bool Equals(ConfigurationIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Environment, other.Environment) && Equals(Structure, other.Structure) && Version == other.Version;
        }

        public static bool operator ==(ConfigurationIdentifier left, ConfigurationIdentifier right) => Equals(left, right);

        public static bool operator !=(ConfigurationIdentifier left, ConfigurationIdentifier right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigurationIdentifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Environment != null ? Environment.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Structure != null ? Structure.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Version.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(Environment)}: {Environment}; {nameof(Structure)}: {Structure}]";
    }
}