using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an Environment with the given identifier has been deleted
    /// </summary>
    public class EnvironmentDeleted : DomainEvent, IEquatable<EnvironmentDeleted>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; init; } = EnvironmentIdentifier.Empty();

        /// <inheritdoc />
        public EnvironmentDeleted()
        {
        }

        /// <inheritdoc />
        public EnvironmentDeleted(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentDeleted? other)
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

            return Equals((EnvironmentDeleted)obj);
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
        public static bool operator ==(EnvironmentDeleted? left, EnvironmentDeleted? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentDeleted? left, EnvironmentDeleted? right) => !Equals(left, right);
    }
}
