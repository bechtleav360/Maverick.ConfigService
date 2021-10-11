using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="ConfigEnvironment" />
    /// </summary>
    public class ConfigEnvironmentMetadata : MetadataBase
    {
        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public EnvironmentIdentifier Id { get; }

        /// <summary>
        ///     Number of Keys contained in this Environment (through assigned Layers)
        /// </summary>
        public int KeyCount { get; set; }

        /// <summary>
        ///     Number of Layers assigned to this Environment
        /// </summary>
        public int LayerCount { get; set; }

        /// <summary>
        ///     List of Layers assigned to this Environment
        /// </summary>
        public List<LayerIdentifier> Layers { get; set; } = new();

        /// <inheritdoc />
        public ConfigEnvironmentMetadata(EnvironmentIdentifier id)
        {
            Id = id;
        }
    }
}
