using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Information to identify an Environment-Layer by
    /// </summary>
    public class LayerIdentifier : Identifier, IEquatable<LayerIdentifier>
    {
        /// <summary>
        ///     Unique Name for a Layer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Creates a new, empty Layer-Identifier
        /// </summary>
        public LayerIdentifier() : this(string.Empty)
        {
        }

        /// <summary>
        ///     Creates a new LayerIdentifier
        /// </summary>
        /// <param name="name"></param>
        public LayerIdentifier(string name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public bool Equals(LayerIdentifier? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
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

            return Equals((LayerIdentifier)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Name);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(LayerIdentifier left, LayerIdentifier right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(LayerIdentifier left, LayerIdentifier right) => !Equals(left, right);

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(LayerIdentifier)}; {nameof(Name)}: '{Name}']";
    }
}
