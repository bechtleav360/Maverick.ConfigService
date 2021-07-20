using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a Layer with accompanying keys has been imported
    /// </summary>
    public class EnvironmentLayerKeysImported : DomainEvent, IEquatable<EnvironmentLayerKeysImported>
    {
        /// <inheritdoc cref="LayerIdentifier" />
        public LayerIdentifier Identifier { get; }

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; }

        /// <inheritdoc />
        public EnvironmentLayerKeysImported(LayerIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public virtual bool Equals(EnvironmentLayerKeysImported other)
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
        public override bool Equals(object obj)
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

            return Equals((EnvironmentLayerKeysImported) obj);
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

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerKeysImported left, EnvironmentLayerKeysImported right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerKeysImported left, EnvironmentLayerKeysImported right) => !Equals(left, right);

        /// <inheritdoc />
        public override IList<DomainEvent> Split()
        {
            // to split the import, we import without keys (deleting everything)
            // and then we "import" the keys by adding several *Modified events
            var list = new List<DomainEvent> {new EnvironmentLayerKeysImported(Identifier, new ConfigKeyAction[0])};

            // double to force floating-point division, so we can round up and not miss any keys during partitioning
            double totalKeys = ModifiedKeys.Length;
            var partitions = 2;
            var keysPerPartition = (int) Math.Ceiling(totalKeys / partitions);

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
