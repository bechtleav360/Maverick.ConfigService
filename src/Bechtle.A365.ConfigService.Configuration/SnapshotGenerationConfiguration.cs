namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     how / when Snapshots of DomainObjects should be projected to the configured Store
    /// </summary>
    public class SnapshotGenerationConfiguration
    {
        /// <inheritdoc cref="SnapshotTriggerConfiguration"/>
        public SnapshotTriggerConfiguration Triggers { get; set; }
    }

    /// <summary>
    ///     configures how a new DomainObject-Snapshot is triggered
    /// </summary>
    public class SnapshotTriggerConfiguration
    {
        /// <inheritdoc cref="NumberThresholdSnapshotConfiguration"/>
        public NumberThresholdSnapshotConfiguration NumberThreshold { get; set; }

        /// <inheritdoc cref="EventBusSnapshotConfiguration"/>
        public EventBusSnapshotConfiguration EventBus { get; set; }
    }

    /// <summary>
    ///     fires when N events have been written to the EventStore since the latest Snapshot
    /// </summary>
    public class NumberThresholdSnapshotConfiguration
    {
        /// <summary>
        ///     Enables / Disables this trigger
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        ///     Sets the number of allowed lag behind the EventStore until a new Snapshot is created
        /// </summary>
        public long Threshold { get; set; } = -1;
    }

    /// <summary>
    ///     fires when a specific EventBus-Message is received
    /// </summary>
    public class EventBusSnapshotConfiguration
    {
        /// <summary>
        ///     Enables / Disables this trigger
        /// </summary>
        public bool Enabled { get; set; } = false;
    }
}