using System;

namespace Bechtle.A365.ConfigService.Projection.Models
{
    /// <summary>
    ///     status of one node working on Config-Events
    /// </summary>
    public class ProjectionNodeStatus
    {
        /// <summary>
        ///     current event that is being projected, if any (in case <see cref="CurrentStatus" /> equals <see cref="ProjectionStatus.Projecting" />)
        /// </summary>
        public ProjectionEventStatus CurrentEvent { get; set; } = new ProjectionEventStatus
        {
            Type = string.Empty,
            Result = EventProjectionResult.Undefined,
            Id = string.Empty,
            Index = 0,
            Event = null,
            At = DateTime.UnixEpoch
        };

        /// <summary>
        ///     current status of the node Working / Idle
        /// </summary>
        public ProjectionStatus CurrentStatus { get; set; } = ProjectionStatus.Idle;

        /// <summary>
        ///     status of the EventStore-Connection
        /// </summary>
        public bool EventStoreConnected { get; set; } = false;

        /// <summary>
        ///     name of the Node
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        ///     how many events are in the Projection-Queue
        /// </summary>
        public long QueueLength { get; set; } = 0;
    }
}