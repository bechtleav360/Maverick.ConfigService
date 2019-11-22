using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public class DefaultEnvironmentCreated : DomainEvent, IEquatable<DefaultEnvironmentCreated>
    {
        /// <inheritdoc />
        public DefaultEnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; }

        public static bool operator ==(DefaultEnvironmentCreated left, DefaultEnvironmentCreated right) => Equals(left, right);

        public static bool operator !=(DefaultEnvironmentCreated left, DefaultEnvironmentCreated right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DefaultEnvironmentCreated) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as DefaultEnvironmentCreated, strict);

        public virtual bool Equals(DefaultEnvironmentCreated other) => Equals(other, false);

        public virtual bool Equals(DefaultEnvironmentCreated other, bool strict)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;

        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };
    }
}