using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a Configuration has been built with information from <see cref="EnvironmentIdentifier" /> and <see cref="StructureIdentifier" />
    /// </summary>
    public class ConfigurationBuilt : DomainEvent, IEquatable<ConfigurationBuilt>
    {
        /// <inheritdoc />
        public ConfigurationBuilt(EnvironmentIdentifier environment,
                                  StructureIdentifier structure,
                                  DateTime? validFrom,
                                  DateTime? validTo)
        {
            Identifier = new ConfigurationIdentifier(environment, structure);
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public ConfigurationIdentifier Identifier { get; }

        /// <summary>
        ///     This Configuration is to be Valid from the given point in time, or always if null
        /// </summary>
        public DateTime? ValidFrom { get; }

        /// <summary>
        ///     This Configuration is to be Valid up to the given point in time, or indefinitely if null
        /// </summary>
        public DateTime? ValidTo { get; }

        public bool Equals(ConfigurationBuilt other) => Equals(other, false);

        public static bool operator ==(ConfigurationBuilt left, ConfigurationBuilt right) => Equals(left, right);

        public static bool operator !=(ConfigurationBuilt left, ConfigurationBuilt right) => !Equals(left, right);

        public bool Equals(ConfigurationBuilt other, bool strict)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return strict
                       ? Equals(Identifier, other.Identifier)
                         && ValidFrom.Equals(other.ValidFrom)
                         && ValidTo.Equals(other.ValidTo)
                       : Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigurationBuilt) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as ConfigurationBuilt, strict);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Identifier != null ? Identifier.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ ValidFrom.GetHashCode();
                hashCode = (hashCode * 397) ^ ValidTo.GetHashCode();
                return hashCode;
            }
        }
    }
}