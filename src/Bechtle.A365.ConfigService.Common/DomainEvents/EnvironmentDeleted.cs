using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     an Environment with the given identifier has been deleted
    /// </summary>
    public class EnvironmentDeleted : DomainEvent, IEquatable<EnvironmentDeleted>
    {
        /// <inheritdoc />
        public EnvironmentDeleted(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public EnvironmentDeleted()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        public bool Equals(EnvironmentDeleted other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public static bool operator ==(EnvironmentDeleted left, EnvironmentDeleted right) => Equals(left, right);

        public static bool operator !=(EnvironmentDeleted left, EnvironmentDeleted right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentDeleted) obj);
        }

        public override bool Equals(DomainEvent other) => Equals(other as EnvironmentDeleted);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}