using Prometheus;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     definitions of all available Metrics for this Service
    /// </summary>
    internal static class KnownMetrics
    {
        /// <summary>
        ///     counts how often Connection-Infos have been retrieved
        /// </summary>
        internal static readonly Counter ConnectionInfo = Metrics.CreateCounter(
            "connection_infos_retrieved",
            "How often Connection-Infos have been retrieved",
            new CounterConfiguration());

        /// <summary>
        ///     counts how often Configurations have been converted from one to another representation
        /// </summary>
        internal static readonly Counter Conversion = Metrics.CreateCounter(
            "convert_configuration",
            "Configurations converted from one to another representation",
            new CounterConfiguration {LabelNames = new[] {"direction"}});

        /// <summary>
        ///     counts how many DomainEvents have been filtered out before bein deserialized
        /// </summary>
        internal static readonly Counter EventsFiltered = Metrics.CreateCounter(
            "domain_events_filtered_before_deserialization",
            "DomainEvents Filtered before Deserialization",
            new CounterConfiguration {LabelNames = new[] { "event_type" }});

        /// <summary>
        ///     counts how many DomainEvents have been read from EventStore
        /// </summary>
        internal static readonly Counter EventsRead = Metrics.CreateCounter(
            "domain_events_read",
            "DomainEvents Read",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        /// <summary>
        ///     counts how many DomainEvents have been streamed from EventStore
        /// </summary>
        internal static readonly Counter EventsStreamed = Metrics.CreateCounter(
            "domain_events_streamed",
            "DomainEvents Streamed",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        /// <summary>
        ///     counts how many connections to the EventStore have been opened
        /// </summary>
        internal static readonly Counter EventStoreConnected = Metrics.CreateCounter(
            "eventstore_connection_established",
            "EventStore Connection established",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many connections to the EventStore have been lost
        /// </summary>
        internal static readonly Counter EventStoreDisconnected = Metrics.CreateCounter(
            "eventstore_connection_lost",
            "EventStore Connection lost",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many connections to the EventStore have been re-opened
        /// </summary>
        internal static readonly Counter EventStoreReconnected = Metrics.CreateCounter(
            "eventstore_connection_reestablished",
            "EventStore Connection Re-Established",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many DomainEvents have been checked for validity
        /// </summary>
        internal static readonly Counter EventsValidated = Metrics.CreateCounter(
            "domain_events_validated",
            "DomainEvents Valildated",
            new CounterConfiguration { LabelNames = new[] { "validity" } });

        /// <summary>
        ///     counts how many DomainEvents have been written to the EventStore
        /// </summary>
        internal static readonly Counter EventsWritten = Metrics.CreateCounter(
            "domain_events_written",
            "DomainEvents Written",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        /// <summary>
        ///     counts how many internal Exceptions have been caught without bubbling up to the User
        /// </summary>
        internal static readonly Counter Exception = Metrics.CreateCounter(
            "internal_exceptions",
            "Internal Exceptions",
            new CounterConfiguration { LabelNames = new[] { "exception_type" } });

        /// <summary>
        ///     counts how many Temporary Keys have been set
        /// </summary>
        internal static readonly Counter TemporaryKeyCreated = Metrics.CreateCounter(
            "temporary_keys_set",
            "Temporary-Key Set",
            new CounterConfiguration());
    }
}