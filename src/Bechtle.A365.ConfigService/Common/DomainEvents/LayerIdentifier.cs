using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify an Environment-Layer by
    /// </summary>
    public class LayerIdentifier : Identifier, IEquatable<LayerIdentifier>
    {
        public LayerIdentifier() : this(string.Empty)
        {
        }

        public LayerIdentifier(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Unique Name for a Layer
        /// </summary>
        public string Name { get; set; }

        public static bool operator ==(LayerIdentifier left, LayerIdentifier right) => Equals(left, right);

        public static bool operator !=(LayerIdentifier left, LayerIdentifier right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LayerIdentifier) obj);
        }

        /// <inheritdoc />
        public bool Equals(LayerIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Name) : 0;

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(LayerIdentifier)}; {nameof(Name)}: '{Name}']";
    }
}