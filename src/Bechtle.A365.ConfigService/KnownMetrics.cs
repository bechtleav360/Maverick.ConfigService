using App.Metrics;
using App.Metrics.Counter;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     definitions of all available Metrics for this Service
    /// </summary>
    internal static class KnownMetrics
    {
        public static readonly CounterOptions ConnectionInfo = new CounterOptions
        {
            Name = "Connection-Infos",
            MeasurementUnit = Unit.Calls,
            Context = KnownMetricContexts.ConnectionInfos
        };

        public static readonly CounterOptions Conversion = new CounterOptions
        {
            Name = "Convert Configuration",
            MeasurementUnit = Unit.Calls,
            Context = KnownMetricContexts.ConversionCalls
        };

        public static readonly CounterOptions EventsRead = new CounterOptions
        {
            Name = "DomainEvents Read",
            MeasurementUnit = Unit.Events,
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventsStreamed = new CounterOptions
        {
            Name = "DomainEvents Streamed",
            MeasurementUnit = Unit.Events,
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventStoreConnected = new CounterOptions
        {
            Name = "EventStore Connection established",
            MeasurementUnit = Unit.Custom("Times"),
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventStoreDisconnected = new CounterOptions
        {
            Name = "EventStore Connection lost",
            MeasurementUnit = Unit.Custom("Times"),
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventStoreReconnected = new CounterOptions
        {
            Name = "EventStore Connection Re-Established",
            MeasurementUnit = Unit.Custom("Times"),
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventsWritten = new CounterOptions
        {
            Name = "DomainEvents Written",
            MeasurementUnit = Unit.Events,
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions EventsWrittenPrevented = new CounterOptions
        {
            Name = "DomainEvents Written (Prevented)",
            MeasurementUnit = Unit.Events,
            Context = KnownMetricContexts.EventStore
        };

        public static readonly CounterOptions Exception = new CounterOptions
        {
            Name = "Internal Exceptions",
            MeasurementUnit = Unit.Items,
            Context = KnownMetricContexts.Exceptions
        };

        public static readonly CounterOptions TemporaryKeyCreated = new CounterOptions
        {
            Name = "Temporary-Key Set",
            MeasurementUnit = Unit.Items,
            Context = KnownMetricContexts.TemporaryKeys
        };
    }

    internal static class KnownMetricContexts
    {
        public static string ConnectionInfos => Generics + ".ConnectionInfos";
        public static string ConversionCalls => Internals + ".Conversions";
        public static string EventStore => Generics + ".EventStore";
        public static string Exceptions => Generics + ".Errors";
        public static string TemporaryKeys => Generics + ".TemporaryKeys";

        private static string Generics => "Application";
        private static string Internals => "Application.Internals";
    }
}