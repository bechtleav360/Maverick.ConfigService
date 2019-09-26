using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    /// <summary>
    ///     snapshot of Structure-data
    /// </summary>
    public class StructureSnapshot
    {
        /// <inheritdoc />
        public StructureSnapshot(StructureIdentifier identifier,
                                 IDictionary<string, string> keys,
                                 IDictionary<string, string> variables)
        {
            Identifier = identifier;
            Data = new ReadOnlyDictionary<string, string>(keys);
            Variables = new ReadOnlyDictionary<string, string>(variables);
        }

        /// <summary>
        ///     key-value pairs containing the structure keys
        /// </summary>
        public IDictionary<string, string> Data { get; }

        /// <summary>
        ///     <see cref="Identifier" /> of the associated data
        /// </summary>
        public StructureIdentifier Identifier { get; }

        /// <summary>
        ///     key-value pairs containing the structures variables
        /// </summary>
        public IDictionary<string, string> Variables { get; }
    }
}