using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a Structure has been deleted
    /// </summary>
    public class StructureDeleted : DomainEvent, IEquatable<StructureDeleted>
    {
        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; }

        /// <inheritdoc />
        public StructureDeleted(StructureIdentifier identifier)
        {
            Identifier = identifier;
        }

        public virtual bool Equals(StructureDeleted other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
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

            return Equals((StructureDeleted) obj);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other, false);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;

        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };

        public static bool operator ==(StructureDeleted left, StructureDeleted right) => Equals(left, right);

        public static bool operator !=(StructureDeleted left, StructureDeleted right) => !Equals(left, right);
    }
}
