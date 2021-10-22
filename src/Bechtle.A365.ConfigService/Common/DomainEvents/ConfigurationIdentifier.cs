using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify a Configuration built from an Environment and a Structure
    /// </summary>
    public class ConfigurationIdentifier : Identifier, IEquatable<ConfigurationIdentifier>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Environment { get; init; }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Structure { get; init; }

        /// <summary>
        ///     Optional version of this Configuration
        /// </summary>
        public long Version { get; init; }

        /// <inheritdoc />
        public ConfigurationIdentifier()
            : this(
                Empty<EnvironmentIdentifier>(),
                Empty<StructureIdentifier>(),
                0)
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
        public virtual bool Equals(ConfigurationIdentifier? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Environment, other.Environment) && Equals(Structure, other.Structure) && Version == other.Version;
        }

        /// <summary>
        ///     Ease-of-Use method for <see cref="Identifier.Empty{T}" />
        /// </summary>
        public static ConfigurationIdentifier Empty() => Empty<ConfigurationIdentifier>();

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ConfigurationIdentifier)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Environment, Structure, Version);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigurationIdentifier? left, ConfigurationIdentifier? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigurationIdentifier? left, ConfigurationIdentifier? right) => !Equals(left, right);

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(Environment)}: {Environment}; {nameof(Structure)}: {Structure}]";
    }
}
