using System;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a number of variables within the Structure have been changed
    /// </summary>
    public class StructureVariablesModified : DomainEvent, IEquatable<StructureVariablesModified>
    {
        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; }

        /// <summary>
        ///     list of actions that have been applied to the variables
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; }

        /// <inheritdoc />
        public StructureVariablesModified(StructureIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public virtual bool Equals(StructureVariablesModified? other)
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

            return Equals((StructureVariablesModified)obj);
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
        public static bool operator ==(StructureVariablesModified? left, StructureVariablesModified? right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(StructureVariablesModified? left, StructureVariablesModified? right) => !Equals(left, right);
    }
}
