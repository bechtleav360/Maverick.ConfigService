using Prometheus;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     definitions of all available Metrics for this Service
    /// </summary>
    internal static class KnownMetrics
    {
        public static readonly Counter ConnectionInfo = Metrics.CreateCounter(
            "connection_infos_retrieved",
            "How often Connection-Infos have been retrieved",
            new CounterConfiguration());

        public static readonly Counter Conversion = Metrics.CreateCounter(
            "convert_configuration",
            "Configurations converted from one to another representation",
            new CounterConfiguration {LabelNames = new[] {"direction"}});

        public static readonly Counter EventsFiltered = Metrics.CreateCounter(
            "domain_events_filtered_before_deserialization",
            "DomainEvents Filtered before Deserialization",
            new CounterConfiguration {LabelNames = new[] { "event_type" }});

        public static readonly Counter EventsRead = Metrics.CreateCounter(
            "domain_events_read",
            "DomainEvents Read",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        public static readonly Counter EventsStreamed = Metrics.CreateCounter(
            "domain_events_streamed",
            "DomainEvents Streamed",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        public static readonly Counter EventStoreConnected = Metrics.CreateCounter(
            "eventstore_connection_established",
            "EventStore Connection established",
            new CounterConfiguration());

        public static readonly Counter EventStoreDisconnected = Metrics.CreateCounter(
            "eventstore_connection_lost",
            "EventStore Connection lost",
            new CounterConfiguration());

        public static readonly Counter EventStoreReconnected = Metrics.CreateCounter(
            "eventstore_connection_reestablished",
            "EventStore Connection Re-Established",
            new CounterConfiguration());

        public static readonly Counter EventsValidated = Metrics.CreateCounter(
            "domain_events_validated",
            "DomainEvents Valildated",
            new CounterConfiguration { LabelNames = new[] { "validity" } });

        public static readonly Counter EventsWritten = Metrics.CreateCounter(
            "domain_events_written",
            "DomainEvents Written",
            new CounterConfiguration { LabelNames = new[] { "event_type" } });

        public static readonly Counter Exception = Metrics.CreateCounter(
            "internal_exceptions",
            "Internal Exceptions",
            new CounterConfiguration { LabelNames = new[] { "exception_type" } });

        public static readonly Counter TemporaryKeyCreated = Metrics.CreateCounter(
            "temporary_keys_set",
            "Temporary-Key Set",
            new CounterConfiguration());
    }
}