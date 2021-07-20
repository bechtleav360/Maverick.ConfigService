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

        /// <inheritdoc />
        public virtual bool Equals(ConfigurationBuilt other) => Equals(other, false);

        /// <inheritdoc />
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

        /// <summary>
        ///     Compare two instances of this DomainEvent for Property-Level equality
        /// </summary>
        /// <param name="other">other instance of this DomainEvent</param>
        /// <param name="strict">compare two instances using strict rules</param>
        /// <returns>true if both objects contain the same data, otherwise false</returns>
        protected virtual bool Equals(ConfigurationBuilt other, bool strict)
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

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigurationBuilt left, ConfigurationBuilt right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigurationBuilt left, ConfigurationBuilt right) => !Equals(left, right);
    }
}
