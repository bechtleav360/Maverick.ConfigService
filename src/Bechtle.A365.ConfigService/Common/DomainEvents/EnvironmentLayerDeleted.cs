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

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentLayerDeleted? other)
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

            return Equals((EnvironmentLayerDeleted)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Identifier.GetHashCode();

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new()
        {
            Filters =
            {
                { KnownDomainEventMetadata.Identifier, Identifier.ToString() }
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerDeleted? left, EnvironmentLayerDeleted? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerDeleted? left, EnvironmentLayerDeleted? right) => !Equals(left, right);
    }
}
