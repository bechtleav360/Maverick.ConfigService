﻿using System;

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

        /// <inheritdoc />
        public StructureDeleted()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }

        public bool Equals(StructureDeleted other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public static bool operator ==(StructureDeleted left, StructureDeleted right) => Equals(left, right);

        public static bool operator !=(StructureDeleted left, StructureDeleted right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructureDeleted) obj);
        }

        public override bool Equals(DomainEvent other) => Equals(other as StructureDeleted);

        public override int GetHashCode() => Identifier != null ? Identifier.GetHashCode() : 0;
    }
}