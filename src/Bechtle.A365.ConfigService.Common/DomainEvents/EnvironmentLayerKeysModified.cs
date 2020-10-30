using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a number of keys within an EnvironmentLayer have been changed
    /// </summary>
    public class EnvironmentLayerKeysModified : DomainEvent, IEquatable<EnvironmentLayerKeysModified>
    {
        /// <inheritdoc />
        public EnvironmentLayerKeysModified(LayerIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; }

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; }

        public static bool operator ==(EnvironmentLayerKeysModified left, EnvironmentLayerKeysModified right) => Equals(left, right);

        public static bool operator !=(EnvironmentLayerKeysModified left, EnvironmentLayerKeysModified right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentLayerKeysModified) obj);
        }

        /// <inheritdoc />
        public override bool Equals(DomainEvent other, bool strict) => Equals(other as EnvironmentLayerKeysModified, strict);

        public virtual bool Equals(EnvironmentLayerKeysModified other) => Equals(other, false);

        public virtual bool Equals(EnvironmentLayerKeysModified other, bool _)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier)
                   && Equals(ModifiedKeys, other.ModifiedKeys);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Identifier != null ? Identifier.GetHashCode() : 0) * 397) ^ (ModifiedKeys != null ? ModifiedKeys.GetHashCode() : 0);
            }
        }

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };
    }
}