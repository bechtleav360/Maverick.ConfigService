using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     Metadata regarding a <see cref="EnvironmentLayer" />
    /// </summary>
    public class EnvironmentLayerMetadata : MetadataBase
    {
        /// <summary>
        ///     List of <see cref="EnvironmentIdentifier" /> that this Layer is assigned to
        /// </summary>
        public List<EnvironmentIdentifier> AssignedTo { get; set; } = new();

        /// <summary>
        ///     Identifier for the DomainObject in question
        /// </summary>
        public LayerIdentifier Id { get; }

        /// <summary>
        ///     Number of Keys contained in this Layer
        /// </summary>
        public int KeyCount { get; set; }

        /// <summary>
        ///     List of Tags attached to this Layer
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <inheritdoc />
        public EnvironmentLayerMetadata(LayerIdentifier id)
        {
            Id = id;
        }
    }
}
