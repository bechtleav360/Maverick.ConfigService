using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an EnvironmentLayer has been created under the given identifier
    /// </summary>
    public class EnvironmentLayerCreated : DomainEvent, IEquatable<EnvironmentLayerCreated>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; init; } = LayerIdentifier.Empty();

        /// <inheritdoc />
        public EnvironmentLayerCreated()
        {
        }

        /// <inheritdoc />
        public EnvironmentLayerCreated(LayerIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentLayerCreated? other)
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

            return Equals((EnvironmentLayerCreated)obj);
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
        public static bool operator ==(EnvironmentLayerCreated? left, EnvironmentLayerCreated? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerCreated? left, EnvironmentLayerCreated? right) => !Equals(left, right);
    }
}
