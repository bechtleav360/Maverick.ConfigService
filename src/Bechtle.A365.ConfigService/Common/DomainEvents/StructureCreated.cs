using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     a Structure has been created with the given <see cref="StructureIdentifier" />
    /// </summary>
    public class StructureCreated : DomainEvent, IEquatable<StructureCreated>
    {
        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; }

        /// <summary>
        ///     keys that make up this Structure
        /// </summary>
        public Dictionary<string, string?> Keys { get; }

        /// <summary>
        ///     variables that may be referenced from Environment or Keys
        /// </summary>
        public Dictionary<string, string?> Variables { get; }

        /// <inheritdoc />
        public StructureCreated(
            StructureIdentifier identifier,
            IDictionary<string, string?> keys,
            IDictionary<string, string?> variables)
        {
            Identifier = identifier;
            Keys = new Dictionary<string, string?>(keys, StringComparer.OrdinalIgnoreCase);
            Variables = new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public virtual bool Equals(StructureCreated? other)
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
                   && CompareDictionaries(Keys, other.Keys)
                   && CompareDictionaries(Variables, other.Variables);
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

            return Equals((StructureCreated)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Identifier, Keys, Variables);

        /// <inheritdoc />
        public override DomainEventMetadata GetMetadata() => new()
        {
            Filters =
            {
                { KnownDomainEventMetadata.Identifier, Identifier.ToString() }
            }
        };

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(StructureCreated left, StructureCreated right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(StructureCreated left, StructureCreated right) => !Equals(left, right);

        private bool CompareDictionaries(IDictionary<string, string?> left, IDictionary<string, string?> right)
            => Equals(left, right)
               || left.Count == right.Count
               && left.All(
                   kvp => right.ContainsKey(kvp.Key)
                          && string.Equals(
                              right[kvp.Key],
                              kvp.Value,
                              StringComparison.OrdinalIgnoreCase));
    }
}
