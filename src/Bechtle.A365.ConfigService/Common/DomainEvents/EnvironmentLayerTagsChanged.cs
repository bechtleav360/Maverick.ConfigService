using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     new Tags were assigned to a Layer
    /// </summary>
    public class EnvironmentLayerTagsChanged : DomainEvent, IEquatable<EnvironmentLayerTagsChanged>
    {
        /// <summary>
        ///     List of tags that were newly assigned to this Layer
        /// </summary>
        public List<string> AddedTags { get; }

        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; }

        /// <summary>
        ///     List of tags that were removed from this Layer
        /// </summary>
        public List<string> RemovedTags { get; }

        /// <inheritdoc />
        public EnvironmentLayerTagsChanged(LayerIdentifier identifier, List<string> addedTags, List<string> removedTags)
        {
            Identifier = identifier;
            AddedTags = addedTags;
            RemovedTags = removedTags;
        }

        /// <inheritdoc />
        public bool Equals(EnvironmentLayerTagsChanged? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Identifier, other.Identifier)
                   && AddedTags.SequenceEqual(other.AddedTags)
                   && RemovedTags.SequenceEqual(other.RemovedTags);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
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

            return Equals((EnvironmentLayerTagsChanged)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Identifier, AddedTags, RemovedTags);

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new()
        {
            Filters =
            {
                { KnownDomainEventMetadata.Identifier, Identifier.ToString() }
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerTagsChanged? left, EnvironmentLayerTagsChanged? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerTagsChanged? left, EnvironmentLayerTagsChanged? right) => !Equals(left, right);
    }
}
