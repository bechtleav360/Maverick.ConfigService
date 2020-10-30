using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a Layer with accompanying keys has been imported
    /// </summary>
    public class EnvironmentLayerKeysImported : DomainEvent, IEquatable<EnvironmentLayerKeysImported>
    {
        /// <inheritdoc />
        public EnvironmentLayerKeysImported(LayerIdentifier identifier, ConfigKeyAction[] modifiedKeys)
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

        public static bool operator ==(EnvironmentLayerKeysImported left, EnvironmentLayerKeysImported right) => Equals(left, right);

        public static bool operator !=(EnvironmentLayerKeysImported left, EnvironmentLayerKeysImported right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnvironmentLayerKeysImported) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as EnvironmentLayerKeysImported, strict);

        public virtual bool Equals(EnvironmentLayerKeysImported other) => Equals(other, false);

        public virtual bool Equals(EnvironmentLayerKeysImported other, bool _)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier)
                   && Equals(ModifiedKeys, other.ModifiedKeys);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Identifier != null ? Identifier.GetHashCode() : 0) * 397) ^ (ModifiedKeys != null ? ModifiedKeys.GetHashCode() : 0);
            }
        }

        public override DomainEventMetadata GetMetadata() => new DomainEventMetadata
        {
            Filters =
            {
                {KnownDomainEventMetadata.Identifier, Identifier.ToString()}
            }
        };
    }
}