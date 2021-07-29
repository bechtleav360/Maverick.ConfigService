using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="EnvironmentLayer"/>
    /// </summary>
    public class EnvironmentLayerMetadata : MetadataBase
    {
        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public LayerIdentifier Id { get; set; }

        /// <summary>
        ///     Number of Keys contained in this Layer
        /// </summary>
        public int KeyCount { get; set; }

        /// <summary>
        ///     List of Tags attached to this Layer
        /// </summary>
        public List<string> Tags { get; set; }
    }
}