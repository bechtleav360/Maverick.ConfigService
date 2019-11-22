using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Formatters;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using App.Metrics.Timer;
using Bechtle.A365.ConfigService.Common.Serialization;

namespace Bechtle.A365.ConfigService
{
    // suppress RedundantAnonymousTypePropertyName warning because we want to be explicit how our Properties are called
    // should they be renamed in the AppMetrics library we're probably screwing our Metrics-Store
    // and they should be the same across all supported services (at least for the generic stuff)
    /// <inheritdoc />
    [SuppressMessage("ReSharper", "RedundantAnonymousTypePropertyName")]
    public class CustomMetricsFormatter : IMetricsOutputFormatter
    {
        /// <inheritdoc />
        public MetricsMediaTypeValue MediaType { get; } = new MetricsMediaTypeValue("application", "bechtle.custom.formatter", "v1", "json");

        /// <inheritdoc />
        public MetricFields MetricFields { get; set; }

        /// <inheritdoc />
        public async Task WriteAsync(Stream output,
                                     MetricsDataValueSource metricsData,
                                     CancellationToken cancellationToken = new CancellationToken())
        {
            // initialize list with at least as many places as counters are given
            // some counters may have .Items which won't be counted here
            var metrics = new List<object>(metricsData.Contexts.Sum(c => c.ApdexScores.Count()
                                                                         + c.Counters.Count()
                                                                         + c.Gauges.Count()
                                                                         + c.Histograms.Count()
                                                                         + c.Meters.Count()
                                                                         + c.Timers.Count()));

            if (cancellationToken.IsCancellationRequested)
                return;

            // for each Metrics-Context, project all *ValueSources into a simpler representation
            // collect all Metrics into a flat list for easier consumption via logstash / other
            try
            {
                metrics.AddRange(
                    metricsData.Contexts
                               .SelectMany(source => new[]
                               {
                                   source.ApdexScores.Select(apdex => TransformApdex(source.Context, apdex)),
                                   source.Counters.Select(counter => TransformCounter(source.Context, counter)),
                                   source.Gauges.Select(gauge => TransformGauge(source.Context, gauge)),
                                   source.Histograms.Select(histogram => TransformHistogram(source.Context,
                                                                                            histogram.Value,
                                                                                            histogram.Name,
                                                                                            histogram.Tags.ToDictionary())),
                                   source.Meters.Select(meter => TransformMeterMetric(source.Context,
                                                                                      meter.Value,
                                                                                      meter.Name,
                                                                                      meter.Tags.ToDictionary())),
                                   source.Timers.Select(timer => TransformTimer(source.Context, timer))
                               }.SelectMany(_ => _)));
            }
            catch (Exception e)
            {
                // don't have a logger to which we could report this,
                // so we throw it with some extra explanation of what could have happened
                throw new MetricsFormatException("unable to collect metrics into POCOs, see inner exception for more details", e);
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            await output.WriteAsync(
                JsonSerializer.SerializeToUtf8Bytes(
                    metrics,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters =
                        {
                            new DoubleConverter(),
                            new FloatConverter(),
                            new JsonIsoDateConverter(),
                            new JsonStringEnumConverter()
                        }
                    }),
                cancellationToken);
        }

        private static object TransformApdex(string context, ApdexValueSource apdex) => new
        {
            ContextName = context,
            Name = apdex.Name,
            Frustrating = apdex.Value.Frustrating,
            SampleSize = apdex.Value.SampleSize,
            Satisfied = apdex.Value.Satisfied,
            Score = apdex.Value.Score,
            Tolerating = apdex.Value.Tolerating,
            Tags = apdex.Tags.ToDictionary()
        };

        private static object TransformCounter(string context, CounterValueSource counter) => new
        {
            ContextName = context,
            Name = counter.Name,
            Count = counter.Value.Count,
            Tags = counter.Tags.ToDictionary(),
            Items = counter.Value
                           .Items
                           .Select(item => new
                           {
                               ContextName = context,
                               Name = counter.Name,
                               Count = item.Count,
                               Item = item.Item,
                               Percent = item.Percent,
                               Tags = item.Tags.ToDictionary()
                           })
                           .ToList()
        };

        private static object TransformGauge(string context, GaugeValueSource gauge) => new
        {
            ContextName = context,
            Name = gauge.Name,
            Value = gauge.Value,
            Tags = gauge.Tags.ToDictionary()
        };

        private static object TransformHistogram(string context, HistogramValue histogram, string name, IDictionary<string, string> tags = null) => new
        {
            ContextName = context,
            Name = name,
            Count = histogram.Count,
            Sum = histogram.Sum,
            LastValue = histogram.LastValue,
            LastUserValue = histogram.LastUserValue,
            Max = histogram.Max,
            MaxUserValue = histogram.MaxUserValue,
            Mean = histogram.Mean,
            Min = histogram.Min,
            MinUserValue = histogram.MinUserValue,
            StdDev = histogram.StdDev,
            Median = histogram.Median,
            Percentile75 = histogram.Percentile75,
            Percentile95 = histogram.Percentile95,
            Percentile98 = histogram.Percentile98,
            Percentile99 = histogram.Percentile99,
            Percentile999 = histogram.Percentile999,
            SampleSize = histogram.SampleSize,
            Tags = tags ?? new Dictionary<string, string>()
        };

        private static object TransformMeterMetric(string context, MeterValue meter, string name, IDictionary<string, string> tags = null) => new
        {
            ContextName = context,
            Name = name,
            Count = meter.Count,
            FifteenMinuteRate = meter.FifteenMinuteRate,
            FiveMinuteRate = meter.FiveMinuteRate,
            Items = meter.Items
                         .Select(item => TransformMeterMetricItem(context, item, name))
                         .ToList(),
            MeanRate = meter.MeanRate,
            OneMinuteRate = meter.OneMinuteRate,
            RateUnit = meter.RateUnit,
            Tags = tags ?? new Dictionary<string, string>()
        };

        private static object TransformMeterMetricItem(string context, MeterValue.SetItem item, string name) => new
        {
            ContextName = context,
            Name = name,
            Item = item.Item,
            Percent = item.Percent,
            Tags = item.Tags.ToDictionary(),
            Value = TransformMeterMetric(context, item.Value, name)
        };

        private static object TransformTimer(string context, TimerValueSource timer) => new
        {
            ContextName = context,
            Name = timer.Name,
            ActiveSessions = timer.Value.ActiveSessions,
            Histogram = TransformHistogram(context, timer.Value.Histogram, timer.Name),
            Rate = TransformMeterMetric(context, timer.Value.Rate, timer.Name),
            DurationUnit = timer.DurationUnit,
            Tags = timer.Tags.ToDictionary()
        };

        /// <summary>
        ///     The Exception that is thrown when formatting MetricsSources to POCOs fails
        /// </summary>
        [Serializable]
        public class MetricsFormatException : Exception
        {
            /// <inheritdoc />
            public MetricsFormatException()
            {
            }

            /// <inheritdoc />
            protected MetricsFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }

            /// <inheritdoc />
            public MetricsFormatException(string message) : base(message)
            {
            }

            /// <inheritdoc />
            public MetricsFormatException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
}