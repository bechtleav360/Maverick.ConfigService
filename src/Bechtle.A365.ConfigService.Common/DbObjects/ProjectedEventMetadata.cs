using System;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    /// <summary>
    ///     information about events that have been projected
    /// </summary>
    public class ProjectedEventMetadata
    {
        /// <summary>
        ///     reported changes that were made in this event
        /// </summary>
        public long Changes { get; set; }

        /// <summary>
        ///     end-time of this event-projection
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        ///     unique id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        ///     original EventIndex
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        ///     true if event could be projected successfully
        /// </summary>
        public bool ProjectedSuccessfully { get; set; }

        /// <summary>
        ///     start-time of this event-projection
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        ///     DomainEvent Type
        /// </summary>
        public string Type { get; set; }
    }
}