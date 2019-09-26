using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent" />
    /// <summary>
    ///     a Structure has been created with the given <see cref="StructureIdentifier" />
    /// </summary>
    public class StructureCreated : DomainEvent, IEquatable<StructureCreated>
    {
        /// <inheritdoc />
        public StructureCreated(StructureIdentifier identifier,
                                IDictionary<string, string> keys,
                                IDictionary<string, string> variables)
        {
            Identifier = identifier;
            Keys = new Dictionary<string, string>(keys, StringComparer.OrdinalIgnoreCase);
            Variables = new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; }

        /// <summary>
        ///     keys that make up this Structure
        /// </summary>
        public Dictionary<string, string> Keys { get; }

        /// <summary>
        ///     variables that may be referenced from Environment or Keys
        /// </summary>
        public Dictionary<string, string> Variables { get; }

        public bool Equals(StructureCreated other) => Equals(other, false);

        public static bool operator ==(StructureCreated left, StructureCreated right) => Equals(left, right);

        public static bool operator !=(StructureCreated left, StructureCreated right) => !Equals(left, right);

        public bool Equals(StructureCreated other, bool strict)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return strict
                       ? Equals(Identifier, other.Identifier)
                         && CompareDictionaries(Keys, other.Keys)
                         && CompareDictionaries(Variables, other.Variables)
                       : Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructureCreated) obj);
        }

        public override bool Equals(DomainEvent other, bool strict) => Equals(other as StructureCreated, strict);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Identifier != null ? Identifier.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Keys != null ? Keys.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Variables != null ? Variables.GetHashCode() : 0);
                return hashCode;
            }
        }

        private bool CompareDictionaries(IDictionary<string, string> left, IDictionary<string, string> right)
            => Equals(left, right) ||
               left.Count == right.Count &&
               left.All(kvp => right.ContainsKey(kvp.Key) &&
                               right[kvp.Key].Equals(kvp.Value, StringComparison.OrdinalIgnoreCase));
    }
}