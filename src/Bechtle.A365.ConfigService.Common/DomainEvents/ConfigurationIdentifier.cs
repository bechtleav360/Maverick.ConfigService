using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify a Configuration built from an Environment and a Structure
    /// </summary>
    public class ConfigurationIdentifier : Identifier, IEquatable<ConfigurationIdentifier>
    {
        /// <inheritdoc />
        public ConfigurationIdentifier(EnvironmentIdentifier environment, StructureIdentifier structure, long version)
        {
            Environment = environment;
            Structure = structure;
            Version = version;
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Environment { get; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Structure { get; }

        /// <summary>
        ///     Optional version of this Configuration
        /// </summary>
        public long Version { get; }

        public static bool operator ==(ConfigurationIdentifier left, ConfigurationIdentifier right) => Equals(left, right);

        public static bool operator !=(ConfigurationIdentifier left, ConfigurationIdentifier right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigurationIdentifier) obj);
        }

        public virtual bool Equals(ConfigurationIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Environment, other.Environment) && Equals(Structure, other.Structure) && Version == other.Version;
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