using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Configuration-Structure, filled out with data from <see cref="StructureIdentifier" /> to create a Configuration
    /// </summary>
    public class StructureIdentifier : Identifier, IEquatable<StructureIdentifier>
    {
        /// <inheritdoc />
        public StructureIdentifier() : this(string.Empty, 0)
        {
        }

        /// <inheritdoc />
        public StructureIdentifier(string name, int version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        ///     name of this structure, indicates uses the Configuration built from this and <see cref="StructureIdentifier" />
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; init; }

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(StructureIdentifier? left, StructureIdentifier? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(StructureIdentifier? left, StructureIdentifier? right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructureIdentifier) obj);
        }

        /// <inheritdoc />
        public virtual bool Equals(StructureIdentifier? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Name, Version);

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(StructureIdentifier)}; {nameof(Name)}: '{Name}'; {nameof(Version)}: '{Version}']";
    }
}
