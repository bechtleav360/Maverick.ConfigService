using System;
using System.Collections.Generic;
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

        /// <inheritdoc cref="ConfigurationIdentifier" />
        public override ConfigurationIdentifier Id { get; set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as JSON
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        ///     Actual Data built from this Configuration, as Key=>Value pair
        /// </summary>
        public IDictionary<string, string> Keys { get; set; }

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

        /// <inheritdoc />
        public PreparedConfiguration()
        {
            Id = null;
            ConfigurationVersion = -1;
            Json = string.Empty;
            Keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            UsedKeys = new List<string>();
            ValidFrom = null;
            ValidTo = null;
        }

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

            Id = identifier;
            ConfigurationVersion = -1;
            Json = string.Empty;
            Keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            UsedKeys = new List<string>();
            ValidFrom = null;
            ValidTo = null;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is PreparedConfiguration other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(ConfigurationVersion, Id, Json, Keys, UsedKeys, ValidFrom, ValidTo);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(PreparedConfiguration left, PreparedConfiguration right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(PreparedConfiguration left, PreparedConfiguration right) => !Equals(left, right);

        private bool Equals(PreparedConfiguration other) =>
            ConfigurationVersion == other.ConfigurationVersion
            && Equals(Id, other.Id)
            && Json == other.Json
            && Equals(Keys, other.Keys)
            && Equals(UsedKeys, other.UsedKeys)
            && Nullable.Equals(ValidFrom, other.ValidFrom)
            && Nullable.Equals(ValidTo, other.ValidTo);
    }
}
