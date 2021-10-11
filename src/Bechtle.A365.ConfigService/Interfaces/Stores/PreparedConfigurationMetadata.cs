using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="PreparedConfiguration" />
    /// </summary>
    public class PreparedConfigurationMetadata : MetadataBase
    {
        /// <inheritdoc />
        public PreparedConfigurationMetadata(ConfigurationIdentifier id)
        {
            Id = id;
        }

        /// <summary>
        ///     Number of Errors found during the compilation of this Configuration
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        ///     Map of Keys and Errors found while resolving its Value
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; set; } = new();

        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public ConfigurationIdentifier Id { get; }

        /// <summary>
        ///     Number of Keys contained in this Configuration
        /// </summary>
        public int KeyCount { get; set; }

        /// <summary>
        ///     List of Keys used during the compilation of this Configuration
        /// </summary>
        public List<string> UsedKeys { get; set; } = new();

        /// <summary>
        ///     Number of Keys used during the compilation of this Configuration
        /// </summary>
        public int UsedKeysCount { get; set; }

        /// <summary>
        ///     Number of Warnings found during the compilation of this Configuration
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        ///     Map of Keys and Warnings found while resolving its Value
        /// </summary>
        public Dictionary<string, List<string>> Warnings { get; set; } = new();
    }
}
