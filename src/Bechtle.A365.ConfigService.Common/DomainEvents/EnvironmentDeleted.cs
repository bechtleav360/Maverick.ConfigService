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

        public bool Equals(EnvironmentDeleted other) => Equals(other, false);

        public static bool operator ==(EnvironmentDeleted left, EnvironmentDeleted right) => Equals(left, right);

        public static bool operator !=(EnvironmentDeleted left, EnvironmentDeleted right) => !Equals(left, right);

        public bool Equals(EnvironmentDeleted other, bool _)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentDeleted) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as EnvironmentDeleted, strict);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}