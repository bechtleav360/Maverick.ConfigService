using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Named Layer of Keys, to be assigned to one or more Environments
    /// </summary>
    public sealed class EnvironmentLayer : DomainObject<LayerIdentifier>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public override LayerIdentifier Id { get; init; } = Identifier.Empty<LayerIdentifier>();

        /// <summary>
        ///     Json-Representation of the actual Data
        /// </summary>
        public string Json { get; init; } = "{}";

        /// <summary>
        ///     Keys represented as nested objects
        /// </summary>
        public List<EnvironmentLayerKeyPath> KeyPaths { get; init; } = new();

        /// <summary>
        ///     Actual Data of this Layer
        /// </summary>
        public Dictionary<string, EnvironmentLayerKey> Keys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     List of Tags assigned to this Layer
        /// </summary>
        public List<string> Tags { get; init; } = new();

        /// <summary>
        ///     List of <see cref="EnvironmentIdentifier" /> that this <see cref="EnvironmentLayer" /> is assigned to
        /// </summary>
        public List<EnvironmentIdentifier> UsedInEnvironments { get; init; } = new();

        /// <inheritdoc />
        public EnvironmentLayer()
        {
        }

        /// <inheritdoc />
        public EnvironmentLayer(LayerIdentifier identifier)
        {
            Id = identifier;
        }

        /// <inheritdoc />
        public EnvironmentLayer(EnvironmentLayer other) : base(other)
        {
            Id = new LayerIdentifier(other.Id.Name);
            Json = other.Json;
            KeyPaths = new List<EnvironmentLayerKeyPath>(other.KeyPaths);
            Keys = new Dictionary<string, EnvironmentLayerKey>(other.Keys, StringComparer.OrdinalIgnoreCase);
            Tags = new List<string>(other.Tags);
            UsedInEnvironments = new List<EnvironmentIdentifier>(other.UsedInEnvironments);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is EnvironmentLayer other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id, Json, Keys, KeyPaths, ChangedAt, ChangedBy, CreatedAt, CreatedBy);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayer left, EnvironmentLayer right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayer left, EnvironmentLayer right) => !Equals(left, right);

        private bool Equals(EnvironmentLayer other) =>
            Equals(Id, other.Id)
            && ChangedAt == other.ChangedAt
            && ChangedBy == other.ChangedBy
            && CreatedAt == other.CreatedAt
            && CreatedBy == other.CreatedBy
            && Json == other.Json
            && Keys.SequenceEqual(other.Keys)
            && KeyPaths.SequenceEqual(other.KeyPaths)
            && UsedInEnvironments.SequenceEqual(other.UsedInEnvironments);
    }
}
