using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a number of variables within the Structure have been changed
    /// </summary>
    public class StructureVariablesModified : DomainEvent, IEquatable<StructureVariablesModified>
    {
        /// <inheritdoc />
        public StructureVariablesModified(StructureIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public StructureVariablesModified()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }

        /// <summary>
        ///     list of actions that have been applied to the variables
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }

        public bool Equals(StructureVariablesModified other) => Equals(other, false);

        public static bool operator ==(StructureVariablesModified left, StructureVariablesModified right) => Equals(left, right);

        public static bool operator !=(StructureVariablesModified left, StructureVariablesModified right) => !Equals(left, right);

        public bool Equals(StructureVariablesModified other, bool _)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier) && Equals(ModifiedKeys, other.ModifiedKeys);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructureVariablesModified) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as StructureVariablesModified, strict);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Identifier != null ? Identifier.GetHashCode() : 0) * 397) ^ (ModifiedKeys != null ? ModifiedKeys.GetHashCode() : 0);
            }
        }
    }
}