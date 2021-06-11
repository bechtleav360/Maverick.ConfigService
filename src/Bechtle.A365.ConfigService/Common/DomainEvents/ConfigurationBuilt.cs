using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a Configuration has been built with information from <see cref="EnvironmentIdentifier" /> and <see cref="StructureIdentifier" />
    /// </summary>
    public class ConfigurationBuilt : DomainEvent, IEquatable<ConfigurationBuilt>
    {
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

        /// <inheritdoc />
        public ConfigurationBuilt(
            ConfigurationIdentifier identifier,
            DateTime? validFrom,
            DateTime? validTo)
        {
            Identifier = identifier;
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        public virtual bool Equals(ConfigurationBuilt other) => Equals(other, false);

        public override bool Equals(object obj)
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

            return Equals((ConfigurationBuilt) obj);
        }

        public virtual bool Equals(ConfigurationBuilt other, bool strict)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return strict
                       ? Equals(Identifier, other.Identifier)
                         && ValidFrom.Equals(other.ValidFrom)
                         && ValidTo.Equals(other.ValidTo)
                       : Equals(Identifier, other.Identifier);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other, false);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Identifier != null ? Identifier.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ ValidFrom.GetHashCode();
                hashCode = (hashCode * 397) ^ ValidTo.GetHashCode();
                return hashCode;
            }
        }

        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };

        public static bool operator ==(ConfigurationBuilt left, ConfigurationBuilt right) => Equals(left, right);

        public static bool operator !=(ConfigurationBuilt left, ConfigurationBuilt right) => !Equals(left, right);
    }
}
