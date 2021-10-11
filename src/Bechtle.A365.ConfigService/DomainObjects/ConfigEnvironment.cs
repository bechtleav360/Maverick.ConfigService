using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Config-Environment which stores a set of Keys from which to build Configurations
    /// </summary>
    public sealed class ConfigEnvironment : DomainObject<EnvironmentIdentifier>
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public override EnvironmentIdentifier Id { get; set; }

        /// <summary>
        ///     Flag indicating if this is the Default-Environment of its Category
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        ///     Json-Representation of the actual Data
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        ///     Keys represented as nested objects
        /// </summary>
        public List<EnvironmentLayerKeyPath> KeyPaths { get; set; }

        /// <summary>
        ///     Actual Data of this Environment
        /// </summary>
        public Dictionary<string, EnvironmentLayerKey> Keys { get; set; }

        /// <summary>
        ///     ordered layers used to represent this Environment
        /// </summary>
        public List<LayerIdentifier> Layers { get; set; }

        /// <inheritdoc />
        public ConfigEnvironment(EnvironmentIdentifier identifier)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (string.IsNullOrWhiteSpace(identifier.Category))
            {
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Category)} is null or empty");
            }

            if (string.IsNullOrWhiteSpace(identifier.Name))
            {
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");
            }

            Id = new EnvironmentIdentifier(identifier.Category, identifier.Name);
            IsDefault = false;
            Json = "{}";
            KeyPaths = new List<EnvironmentLayerKeyPath>();
            Keys = new Dictionary<string, EnvironmentLayerKey>(StringComparer.OrdinalIgnoreCase);
            Layers = new List<LayerIdentifier>();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is ConfigEnvironment other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id, IsDefault, Layers, ChangedAt, ChangedBy, CreatedAt, CreatedBy);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(ConfigEnvironment left, ConfigEnvironment right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(ConfigEnvironment left, ConfigEnvironment right) => !Equals(left, right);

        private bool Equals(ConfigEnvironment other) =>
            Equals(Id, other.Id)
            && ChangedAt == other.ChangedAt
            && ChangedBy == other.ChangedBy
            && CreatedAt == other.CreatedAt
            && CreatedBy == other.CreatedBy
            && IsDefault == other.IsDefault
            && Json == other.Json
            && KeyPaths.SequenceEqual(other.KeyPaths)
            && Keys.SequenceEqual(other.Keys)
            && Layers.SequenceEqual(other.Layers);
    }
}
