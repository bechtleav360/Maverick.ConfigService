using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="ConfigStructure" />
    /// </summary>
    public class ConfigStructureMetadata : MetadataBase
    {
        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public StructureIdentifier Id { get; }

        /// <summary>
        ///     Number of Keys contained in this Structure
        /// </summary>
        public int KeyCount { get; set; }

        /// <summary>
        ///     Number of Variables contained in this Structure
        /// </summary>
        public int VariablesCount { get; set; }

        /// <inheritdoc />
        public ConfigStructureMetadata(StructureIdentifier id)
        {
            Id = id;
        }
    }
}
