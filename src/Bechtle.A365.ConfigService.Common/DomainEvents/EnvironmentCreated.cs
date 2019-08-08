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

        public bool Equals(EnvironmentCreated other) => Equals(other, false);

        public static bool operator ==(EnvironmentCreated left, EnvironmentCreated right) => Equals(left, right);

        public static bool operator !=(EnvironmentCreated left, EnvironmentCreated right) => !Equals(left, right);

        public bool Equals(EnvironmentCreated other, bool _)
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
            return Equals((EnvironmentCreated) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as EnvironmentCreated, strict);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}