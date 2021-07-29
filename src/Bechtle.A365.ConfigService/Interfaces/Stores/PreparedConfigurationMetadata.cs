using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="PreparedConfiguration"/>
    /// </summary>
    public class PreparedConfigurationMetadata : MetadataBase
    {
        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public ConfigurationIdentifier Id { get; set; }

        /// <summary>
        ///     Number of Keys contained in this Configuration
        /// </summary>
        public int KeyCount { get; set; }
    }
}