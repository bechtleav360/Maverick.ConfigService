using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     A Default-Environment has been Created
    /// </summary>
    public class DefaultEnvironmentCreated : DomainEvent, IEquatable<DefaultEnvironmentCreated>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; init; } = EnvironmentIdentifier.Empty();

        /// <inheritdoc />
        public DefaultEnvironmentCreated()
        {
        }

        /// <inheritdoc />
        public DefaultEnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public virtual bool Equals(DefaultEnvironmentCreated? other)
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

            return Equals((DefaultEnvironmentCreated)obj);
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
        public static bool operator ==(DefaultEnvironmentCreated? left, DefaultEnvironmentCreated? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(DefaultEnvironmentCreated? left, DefaultEnvironmentCreated? right) => !Equals(left, right);
    }
}
