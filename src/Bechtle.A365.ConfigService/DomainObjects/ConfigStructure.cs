using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Structure for a Configuration
    /// </summary>
    public sealed class ConfigStructure : DomainObject<StructureIdentifier>
    {
        /// <inheritdoc />
        public ConfigStructure(StructureIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");

            if (identifier.Version <= 0)
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Version)} is null or empty");

            Id = new StructureIdentifier(identifier.Name, identifier.Version);
            Keys = new Dictionary<string, string>();
            Variables = new Dictionary<string, string>();
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public override StructureIdentifier Id { get; set; }

        /// <summary>
        ///     Dictionary containing all hard-coded Values and References to the Environment-Data
        /// </summary>
        public Dictionary<string, string> Keys { get; set; }

        /// <summary>
        ///     Modifiable Variables that may be used inside the Structure during Compilation
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }
}
