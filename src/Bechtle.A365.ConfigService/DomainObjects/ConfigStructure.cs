using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Structure for a Configuration
    /// </summary>
    public sealed class ConfigStructure : DomainObject<StructureIdentifier>
    {
        /// <inheritdoc cref="StructureIdentifier" />
        public override StructureIdentifier Id { get; init; } = Identifier.Empty<StructureIdentifier>();

        /// <summary>
        ///     Dictionary containing all hard-coded Values and References to the Environment-Data
        /// </summary>
        public Dictionary<string, string?> Keys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Modifiable Variables that may be used inside the Structure during Compilation
        /// </summary>
        public Dictionary<string, string?> Variables { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public ConfigStructure()
        {
        }

        /// <inheritdoc />
        public ConfigStructure(StructureIdentifier identifier)
        {
            Id = identifier;
        }

        /// <inheritdoc />
        public ConfigStructure(ConfigStructure other) : base(other)
        {
            Id = new StructureIdentifier(other.Id.Name, other.Id.Version);
            Keys = new Dictionary<string, string?>(other.Keys, StringComparer.OrdinalIgnoreCase);
            Variables = new Dictionary<string, string?>(other.Variables, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is ConfigStructure other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id, Keys, Variables, ChangedAt, ChangedBy, CreatedAt, CreatedBy);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigStructure left, ConfigStructure right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigStructure left, ConfigStructure right) => !Equals(left, right);

        private bool Equals(ConfigStructure other) =>
            Equals(Id, other.Id)
            && ChangedAt == other.ChangedAt
            && ChangedBy == other.ChangedBy
            && CreatedAt == other.CreatedAt
            && CreatedBy == other.CreatedBy
            && Keys.SequenceEqual(other.Keys)
            && Variables.SequenceEqual(other.Variables);
    }
}
