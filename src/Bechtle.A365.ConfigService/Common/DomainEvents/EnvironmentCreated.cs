using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an Environment has been created under the given identifier
    /// </summary>
    public class EnvironmentCreated : DomainEvent, IEquatable<EnvironmentCreated>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; }

        /// <inheritdoc />
        public EnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        public virtual bool Equals(EnvironmentCreated other)
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

            return Equals((EnvironmentCreated) obj);
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

        public static bool operator ==(EnvironmentCreated left, EnvironmentCreated right) => Equals(left, right);

        public static bool operator !=(EnvironmentCreated left, EnvironmentCreated right) => !Equals(left, right);
    }
}
