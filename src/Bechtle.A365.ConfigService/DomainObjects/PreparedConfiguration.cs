using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Configuration built from a Structure and an Environment
    /// </summary>
    public sealed class PreparedConfiguration : DomainObject<ConfigurationIdentifier>
    {
        /// <summary>
        ///     Data-Version from which this Configuration was built
        /// </summary>
        public long ConfigurationVersion { get; init; }

        /// <summary>
        ///     Map of Keys and their associated Errors
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public override ConfigurationIdentifier Id { get; init; } = Identifier.Empty<ConfigurationIdentifier>();

        /// <summary>
        ///     Actual Data built from this Configuration, as JSON
        /// </summary>
        public string Json { get; init; } = "{}";

        /// <summary>
        ///     Actual Data built from this Configuration, as Key=>Value pair
        /// </summary>
        public Dictionary<string, string?> Keys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     List of Environment-Keys used to build this Configuration
        /// </summary>
        public List<string> UsedKeys { get; init; } = new();

        /// <summary>
        ///     Starting-Time from which this Configuration is Valid
        /// </summary>
        public DateTime? ValidFrom { get; init; }

        /// <summary>
        ///     End-Time until which this Configuration is Valid
        /// </summary>
        public DateTime? ValidTo { get; init; }

        /// <summary>
        ///     Map of Keys and their associated Warnings
        /// </summary>
        public Dictionary<string, List<string>> Warnings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public PreparedConfiguration()
        {
        }

        /// <inheritdoc />
        public PreparedConfiguration(ConfigurationIdentifier identifier)
        {
            Id = identifier;
        }

        /// <inheritdoc />
        public PreparedConfiguration(PreparedConfiguration other) : base(other)
        {
            ConfigurationVersion = other.ConfigurationVersion;
            Errors = new Dictionary<string, List<string>>(other.Errors, StringComparer.OrdinalIgnoreCase);
            Id = new ConfigurationIdentifier(
                new EnvironmentIdentifier(other.Id.Environment.Category, other.Id.Environment.Name),
                new StructureIdentifier(other.Id.Structure.Name, other.Id.Structure.Version),
                other.Id.Version);
            Json = other.Json;
            Keys = new Dictionary<string, string?>(other.Keys, StringComparer.OrdinalIgnoreCase);
            UsedKeys = new List<string>(other.UsedKeys);
            ValidFrom = other.ValidFrom;
            ValidTo = other.ValidTo;
            Warnings = new Dictionary<string, List<string>>(other.Warnings, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is PreparedConfiguration other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(ConfigurationVersion, Id, ValidFrom, ValidTo, ChangedAt, ChangedBy, CreatedAt, CreatedBy);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(PreparedConfiguration left, PreparedConfiguration right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(PreparedConfiguration left, PreparedConfiguration right) => !Equals(left, right);

        private bool Equals(PreparedConfiguration other) =>
            ConfigurationVersion == other.ConfigurationVersion
            && Equals(Id, other.Id)
            && Errors.SequenceEqual(other.Errors)
            && ChangedAt == other.ChangedAt
            && ChangedBy == other.ChangedBy
            && CreatedAt == other.CreatedAt
            && CreatedBy == other.CreatedBy
            && Json == other.Json
            && Equals(Keys, other.Keys)
            && Equals(UsedKeys, other.UsedKeys)
            && Nullable.Equals(ValidFrom, other.ValidFrom)
            && Nullable.Equals(ValidTo, other.ValidTo)
            && Warnings.SequenceEqual(other.Warnings);
    }
}
