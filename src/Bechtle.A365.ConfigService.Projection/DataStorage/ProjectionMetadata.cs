using System;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class ProjectionMetadata
    {
        public Guid Id { get; set; }

        /// <summary>
        ///     null to indicate that no events have been projected
        /// </summary>
        public long? LatestEvent { get; set; }

        /// <summary>
        ///     Id of the last known active Configuration
        ///     if this changes an OnConfigurationPublished event should be published containing the new configuration-information
        /// </summary>
        public Guid LastActiveConfigurationId { get; set; }
    }
}