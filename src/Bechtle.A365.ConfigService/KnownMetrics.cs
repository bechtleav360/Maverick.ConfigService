using App.Metrics;
using App.Metrics.Counter;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     definitions of all available Metrics for this Service
    /// </summary>
    internal static class KnownMetrics
    {
        public static readonly CounterOptions Exception = new CounterOptions
        {
            Name = "Internal Exceptions",
            MeasurementUnit = Unit.Items,
            Context = KnownMetricContexts.Exceptions
        };

        public static readonly CounterOptions Conversion = new CounterOptions
        {
            Name = "Convert Configuration",
            MeasurementUnit = Unit.Calls,
            Context = KnownMetricContexts.ConversionCalls
        };
    }

    internal static class KnownMetricContexts
    {
        public static string ConversionCalls => Internals + ".Conversions";
        public static string Exceptions => Generics + ".Errors";

        private static string Generics => "Application";
        private static string Internals => "Application.Internals";
    }
}