using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     an EnvironmentLayer has been copied along all of its Data, but with a new Name
    /// </summary>
    public sealed class EnvironmentLayerCopied : DomainEvent, IEquatable<EnvironmentLayerCopied>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier SourceIdentifier { get; }

        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier TargetIdentifier { get; }

        /// <inheritdoc />
        public EnvironmentLayerCopied(LayerIdentifier sourceIdentifier, LayerIdentifier targetIdentifier)
        {
            SourceIdentifier = sourceIdentifier;
            TargetIdentifier = targetIdentifier;
        }

        /// <inheritdoc />
        public bool Equals(EnvironmentLayerCopied? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(SourceIdentifier, other.SourceIdentifier)
                   && Equals(TargetIdentifier, other.TargetIdentifier);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) || obj is EnvironmentLayerCopied other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(SourceIdentifier, TargetIdentifier);

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new()
        {
            Filters =
            {
                { KnownDomainEventMetadata.Identifier, SourceIdentifier.ToString() }
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerCopied left, EnvironmentLayerCopied right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerCopied left, EnvironmentLayerCopied right) => !Equals(left, right);
    }
}
