using App.Metrics;
using App.Metrics.Gauge;
using Bechtle.A365.ConfigService.Projection.Metrics;

namespace Bechtle.A365.ConfigService.Projection.Extensions
{
    public static class MetricsExtensions
    {
        public static IMetrics RegisterFailure(this IMetrics metrics, MetricTags endpointTags, MetricTags transactionTags)
        {
            metrics.Measure.Counter.Increment(KnownMetrics.TotalErrorRequestCount);
            metrics.Measure.Meter.Mark(KnownMetrics.EndpointErrorRequestRate, endpointTags);
            metrics.Measure.Meter.Mark(KnownMetrics.ErrorRequestRate, transactionTags);

            metrics.Measure
                   .Gauge
                   .SetValue(KnownMetrics.EndpointOneMinuteErrorPercentageRate,
                             endpointTags,
                             () => new HitPercentageGauge(metrics.Provider.Meter.Instance(KnownMetrics.EndpointErrorRequestRate, endpointTags),
                                                          metrics.Provider.Timer.Instance(KnownMetrics.EndpointRequestTransactionDuration, endpointTags),
                                                          m => m.OneMinuteRate));

            metrics.Measure
                   .Gauge
                   .SetValue(KnownMetrics.OneMinErrorPercentageRate,
                             () => new HitPercentageGauge(metrics.Provider.Meter.Instance(KnownMetrics.ErrorRequestRate),
                                                          metrics.Provider.Timer.Instance(KnownMetrics.RequestTransactionDuration),
                                                          m => m.OneMinuteRate));

            return metrics;
        }

        public static MetricTags CreateTransactionTags(string routeName)
            => new MetricTags("route", routeName);

        public static MetricTags CreateEndpointTags(string routeName, string endpoint)
            => new MetricTags(new[] {"route", "endpoint"},
                              new[] {routeName, endpoint});
    }
}