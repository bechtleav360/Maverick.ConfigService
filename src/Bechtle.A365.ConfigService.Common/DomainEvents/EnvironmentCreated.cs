using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     an Environment has been created under the given identifier
    /// </summary>
    public class EnvironmentCreated : DomainEvent, IEquatable<EnvironmentCreated>
    {
        /// <inheritdoc />
        public EnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public EnvironmentCreated()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        public bool Equals(EnvironmentCreated other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public static bool operator ==(EnvironmentCreated left, EnvironmentCreated right) => Equals(left, right);

        public static bool operator !=(EnvironmentCreated left, EnvironmentCreated right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentCreated) obj);
        }

        public override bool Equals(DomainEvent other) => Equals(other as EnvironmentCreated);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}