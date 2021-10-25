using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a number of keys within an EnvironmentLayer have been changed
    /// </summary>
    public class EnvironmentLayerKeysModified : DomainEvent, IEquatable<EnvironmentLayerKeysModified>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; init; } = LayerIdentifier.Empty();

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; init; } = Array.Empty<ConfigKeyAction>();

        /// <inheritdoc />
        public EnvironmentLayerKeysModified()
        {
        }

        /// <inheritdoc />
        public EnvironmentLayerKeysModified(LayerIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentLayerKeysModified? other)
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
                   && ModifiedKeys.SequenceEqual(other.ModifiedKeys);
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

            return Equals((EnvironmentLayerKeysModified)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Identifier.GetHashCode() * 397) ^ ModifiedKeys.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new()
        {
            Filters =
            {
                { KnownDomainEventMetadata.Identifier, Identifier.ToString() }
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerKeysModified? left, EnvironmentLayerKeysModified? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerKeysModified? left, EnvironmentLayerKeysModified? right) => !Equals(left, right);

        /// <inheritdoc />
        public override IList<DomainEvent> Split()
        {
            var list = new List<DomainEvent>();

            // double to force floating-point division, so we can round up and not miss any keys during partitioning
            double totalKeys = ModifiedKeys.Length;
            var partitions = 2;
            var keysPerPartition = (int)Math.Ceiling(totalKeys / partitions);

            var counter = 0;
            var nextImport = new List<ConfigKeyAction>();
            foreach (ConfigKeyAction action in ModifiedKeys)
            {
                nextImport.Add(action);
                ++counter;

                if (counter > keysPerPartition)
                {
                    counter = 0;
                    list.Add(new EnvironmentLayerKeysModified(Identifier, nextImport.ToArray()));
                    nextImport = new List<ConfigKeyAction>();
                }
            }

            // add the keys that might not have been added during the loop
            if (nextImport.Any())
            {
                list.Add(new EnvironmentLayerKeysModified(Identifier, nextImport.ToArray()));
                nextImport.Clear();
            }

            return list;
        }
    }
}
