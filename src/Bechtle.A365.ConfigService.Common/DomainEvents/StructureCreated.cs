using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Structure has been created with the given <see cref="StructureIdentifier" />
    /// </summary>
    public class StructureCreated : DomainEvent
    {
        /// <inheritdoc />
        public StructureCreated(StructureIdentifier identifier,
                                IDictionary<string, string> keys,
                                IDictionary<string, string> variables)
        {
            Identifier = identifier;
            Keys = new Dictionary<string, string>(keys);
            Variables = new Dictionary<string, string>(variables);
        }

        /// <inheritdoc />
        public StructureCreated()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }

        /// <summary>
        ///     keys that make up this Structure
        /// </summary>
        public Dictionary<string, string> Keys { get; set; }

        /// <summary>
        ///     variables that may be referenced from Environment or Keys
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }
}