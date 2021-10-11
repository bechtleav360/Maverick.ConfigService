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
        public long ConfigurationVersion { get; set; }

        /// <summary>
        ///     Map of Keys and their associated Errors
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; set; }

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public override ConfigurationIdentifier Id { get; set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as JSON
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as Key=>Value pair
        /// </summary>
        public IDictionary<string, string?> Keys { get; set; }

        /// <summary>
        ///     List of Environment-Keys used to build this Configuration
        /// </summary>
        public List<string> UsedKeys { get; set; }

        /// <summary>
        ///     Starting-Time from which this Configuration is Valid
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        ///     End-Time until which this Configuration is Valid
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        ///     Map of Keys and their associated Warnings
        /// </summary>
        public Dictionary<string, List<string>> Warnings { get; set; }

        /// <inheritdoc />
        public PreparedConfiguration(ConfigurationIdentifier identifier)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (identifier.Environment is null)
            {
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Environment)} is null");
            }

            if (identifier.Structure is null)
            {
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Structure)} is null");
            }

            Errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Id = identifier;
            Json = "{}";
            Keys = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            UsedKeys = new List<string>();
            ValidFrom = null;
            ValidTo = null;
            Warnings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
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
