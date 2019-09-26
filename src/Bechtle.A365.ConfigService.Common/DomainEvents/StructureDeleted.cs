using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a Structure has been deleted
    /// </summary>
    public class StructureDeleted : DomainEvent, IEquatable<StructureDeleted>
    {
        /// <inheritdoc />
        public StructureDeleted(StructureIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; }

        public bool Equals(StructureDeleted other) => Equals(other, false);

        public static bool operator ==(StructureDeleted left, StructureDeleted right) => Equals(left, right);

        public static bool operator !=(StructureDeleted left, StructureDeleted right) => !Equals(left, right);

        public bool Equals(StructureDeleted other, bool _)
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
            return Equals((StructureDeleted) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as StructureDeleted, strict);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}