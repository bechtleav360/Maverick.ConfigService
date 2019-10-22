﻿using System;
using Bechtle.A365.ConfigService.Common.DbObjects;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Configuration-Structure, filled out with data from <see cref="StructureIdentifier" /> to create a Configuration
    /// </summary>
    public class StructureIdentifier : Identifier, IEquatable<StructureIdentifier>
    {
        /// <inheritdoc />
        public StructureIdentifier(string name, int version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        ///     name of this structure, indicates uses the Configuration built from this and <see cref="StructureIdentifier" />
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; }

        public bool Equals(StructureIdentifier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && Version == other.Version;
        }

        /// <summary>
        ///     construct a new <see cref="StructureIdentifier" /> from the values in the given <paramref name="structure" />
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static StructureIdentifier From(Structure structure) => new StructureIdentifier(structure?.Name, structure?.Version ?? 0);

        public static bool operator ==(StructureIdentifier left, StructureIdentifier right) => Equals(left, right);

        public static bool operator !=(StructureIdentifier left, StructureIdentifier right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructureIdentifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Version;
            }
        }

        public override string ToString() => $"[{nameof(StructureIdentifier)}; {nameof(Name)}: '{Name}'; {nameof(Version)}: '{Version}']";
    }
}