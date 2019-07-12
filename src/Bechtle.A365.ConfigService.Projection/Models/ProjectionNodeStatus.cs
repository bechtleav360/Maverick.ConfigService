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
        public ProjectionEventStatus CurrentEvent { get; set; }

        /// <summary>
        ///     current status of the node Working / Idle
        /// </summary>
        public ProjectionStatus CurrentStatus { get; set; }

        /// <summary>
        ///     status of the EventStore-Connection
        /// </summary>
        public bool EventStoreConnected { get; set; }

        /// <summary>
        ///     name of the Node
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        ///     how many events are in the Projection-Queue
        /// </summary>
        public long QueueLength { get; set; }
    }
}