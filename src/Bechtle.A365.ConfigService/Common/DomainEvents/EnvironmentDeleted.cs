using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an Environment with the given identifier has been deleted
    /// </summary>
    public class EnvironmentDeleted : DomainEvent, IEquatable<EnvironmentDeleted>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; }

        /// <inheritdoc />
        public EnvironmentDeleted(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        public virtual bool Equals(EnvironmentDeleted other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Identifier, other.Identifier);
        }

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

            return Equals((EnvironmentDeleted) obj);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other, false);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;

        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };

        public static bool operator ==(EnvironmentDeleted left, EnvironmentDeleted right) => Equals(left, right);

        public static bool operator !=(EnvironmentDeleted left, EnvironmentDeleted right) => !Equals(left, right);
    }
}
