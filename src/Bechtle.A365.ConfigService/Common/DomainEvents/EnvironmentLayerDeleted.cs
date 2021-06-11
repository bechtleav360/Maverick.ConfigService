using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an EnvironmentLayer with the given identifier has been deleted
    /// </summary>
    public class EnvironmentLayerDeleted : DomainEvent, IEquatable<EnvironmentLayerDeleted>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; }

        /// <inheritdoc />
        public EnvironmentLayerDeleted(LayerIdentifier identifier)
        {
            Identifier = identifier;
        }

        public virtual bool Equals(EnvironmentLayerDeleted other)
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

            return Equals((EnvironmentLayerDeleted) obj);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other, false);

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

        public static bool operator ==(EnvironmentLayerDeleted left, EnvironmentLayerDeleted right) => Equals(left, right);

        public static bool operator !=(EnvironmentLayerDeleted left, EnvironmentLayerDeleted right) => !Equals(left, right);
    }
}
