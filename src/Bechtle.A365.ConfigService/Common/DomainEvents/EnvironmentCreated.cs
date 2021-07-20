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

        /// <inheritdoc />
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

            return Equals((EnvironmentCreated) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentCreated left, EnvironmentCreated right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentCreated left, EnvironmentCreated right) => !Equals(left, right);
    }
}
