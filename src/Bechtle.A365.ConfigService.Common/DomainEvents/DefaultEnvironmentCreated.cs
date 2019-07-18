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

        /// <inheritdoc />
        public DefaultEnvironmentCreated()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        public bool Equals(DefaultEnvironmentCreated other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public static bool operator ==(DefaultEnvironmentCreated left, DefaultEnvironmentCreated right) => Equals(left, right);

        public static bool operator !=(DefaultEnvironmentCreated left, DefaultEnvironmentCreated right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DefaultEnvironmentCreated) obj);
        }

        public override bool Equals(DomainEvent other) => Equals(other as DefaultEnvironmentCreated);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}