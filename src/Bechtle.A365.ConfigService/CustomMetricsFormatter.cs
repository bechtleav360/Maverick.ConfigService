using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bechtle.A365.ConfigService
{
    /// <inheritdoc />
    public class CustomMetricsFormatter : IMetricsOutputFormatter
    {
        /// <inheritdoc />
        public MetricsMediaTypeValue MediaType { get; } = new MetricsMediaTypeValue("text", "bechtle.custom.formatter", "v1", "plain");

        /// <inheritdoc />
        public MetricFields MetricFields { get; set; }

        /// <inheritdoc />
        public async Task WriteAsync(Stream output,
                                     MetricsDataValueSource metricsData,
                                     CancellationToken cancellationToken = new CancellationToken())
        {
            var metrics = new List<ServiceMetric>();

            foreach (var contextSource in metricsData.Contexts)
            {
                metrics.AddRange(contextSource.ApdexScores.Select(apdex => new ServiceApdexMetric(contextSource.Context, apdex)));
                metrics.AddRange(contextSource.Counters.Select(counter => new ServiceCounterMetric(contextSource.Context, counter)));
                metrics.AddRange(contextSource.Gauges.Select(gauge => new ServiceGaugeMetric(contextSource.Context, gauge)));
                metrics.AddRange(contextSource.Histograms.Select(histogram => new ServiceHistogramMetric(contextSource.Context, histogram)));
                metrics.AddRange(contextSource.Meters.Select(meter => new ServiceMeterMetric(contextSource.Context, meter)));
                metrics.AddRange(contextSource.Timers.Select(timer => new ServiceTimerMetric(contextSource.Context, timer)));
            }

            metrics = metrics.OrderBy(m => m.ContextName)
                             .ThenBy(m => m.Name)
                             .ThenBy(m => m.GetType().Name)
                             .ToList();

            // specify that encoding should *not* write UTF-8 BOM
            using (var writer = new StreamWriter(output, new UTF8Encoding(false)))
            {
                var json = string.Empty;

                try
                {
                    json = JsonConvert.SerializeObject(metrics, new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        FloatFormatHandling = FloatFormatHandling.DefaultValue,
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        Converters =
                        {
                            new StringEnumConverter(),
                            new IsoDateTimeConverter()
                        }
                    });
                }
                catch (JsonException)
                {
                    json = "{ }";
                }

                await writer.WriteAsync(json);
            }
        }
    }

    /// <summary>
    ///     base class for all items that <see cref="CustomMetricsFormatter" /> returns
    /// </summary>
    public abstract class ServiceMetric
    {
        /// <inheritdoc />
        protected ServiceMetric(string contextName, string name)
        {
            ContextName = contextName;
            Name = name;
        }

        /// <summary>
        ///     Name of the associated Metric-Context / Area this Metric is assigned to
        /// </summary>
        public string ContextName { get; set; }

        /// <summary>
        ///     Specific Name of this Metric
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     List of tags (+values) added to this Metric
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    ///     representation of <see cref="CounterValueSource" /> items
    /// </summary>
    public class ServiceCounterMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceCounterMetric(string context, CounterValueSource counter)
            : base(context, counter.Name)
        {
            ContextName = context;
            Count = counter.Value.Count;
            Items = counter.Value
                           .Items
                           .Select(i => new ServiceCounterSetItem(i))
                           .ToList();
        }

        /// <summary>
        ///     Gets the total count of the counter instance.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        ///     Gets counters for each registered set item.
        /// </summary>
        public List<ServiceCounterSetItem> Items { get; set; }
    }

    /// <summary>
    ///     Specific item counted
    /// </summary>
    public class ServiceCounterSetItem
    {
        /// <inheritdoc />
        public ServiceCounterSetItem(CounterValue.SetItem item)
        {
            Count = item.Count;
            Item = item.Item;
            Percent = item.Percent;
            Tags = item.Tags.ToDictionary();
        }

        /// <summary>
        ///     Gets the specific count for this item.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        ///     Gets the registered item name.
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///     Gets the percent of this item from the total count.
        /// </summary>
        public double Percent { get; set; }

        /// <summary>
        ///     Tags associated with this Item
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }
    }

    /// <summary>
    ///     representation of <see cref="ApdexValueSource" /> items
    ///     The Apdex score is calculated based on your required SLA (Service-Level Agreement)
    ///     where you can define a response time threshold of T seconds,
    ///     where all responses handled in T or less seconds satisfy the end user.
    /// </summary>
    public class ServiceApdexMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceApdexMetric(string contextName, ApdexValueSource apdex)
            : base(contextName, apdex.Name)
        {
            Frustrating = apdex.Value.Frustrating;
            SampleSize = apdex.Value.SampleSize;
            Satisfied = apdex.Value.Satisfied;
            Score = apdex.Value.Score;
            Tolerating = apdex.Value.Tolerating;
        }

        /// <summary>
        ///     Response time greater than 4 T seconds
        /// </summary>
        public int Frustrating { get; set; }

        /// <summary>
        ///     Number of Samples to calculate Scores
        /// </summary>
        public int SampleSize { get; set; }

        /// <summary>
        ///     Response time less than or equal to T seconds
        /// </summary>
        public int Satisfied { get; set; }

        /// <summary>
        ///     Overall Apdex-Score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        ///     Response time between T seconds and 4T seconds
        /// </summary>
        public int Tolerating { get; set; }
    }

    /// <summary>
    ///     representation of <see cref="GaugeValueSource" /> items
    /// </summary>
    public class ServiceGaugeMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceGaugeMetric(string contextName, GaugeValueSource gauge)
            : base(contextName, gauge.Name)
        {
            Value = gauge.Value;
        }

        /// <summary>
        ///     current value of this Gauge
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    ///     representation of <see cref="HistogramValueSource" /> items
    /// </summary>
    public class ServiceHistogramMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceHistogramMetric(string contextName, HistogramValueSource histogram)
            : this(contextName, histogram.Value, histogram.Name)
        {
        }

        /// <inheritdoc />
        public ServiceHistogramMetric(string contextName, HistogramValue histogram, string name)
            : base(contextName, name)
        {
            Count = histogram.Count;
            Sum = histogram.Sum;
            LastValue = histogram.LastValue;
            LastUserValue = histogram.LastUserValue;
            Max = histogram.Max;
            MaxUserValue = histogram.MaxUserValue;
            Mean = histogram.Mean;
            Min = histogram.Min;
            MinUserValue = histogram.MinUserValue;
            StdDev = histogram.StdDev;
            Median = histogram.Median;
            Percentile75 = histogram.Percentile75;
            Percentile95 = histogram.Percentile95;
            Percentile98 = histogram.Percentile98;
            Percentile99 = histogram.Percentile99;
            Percentile999 = histogram.Percentile999;
            SampleSize = histogram.SampleSize;
        }

        /// <summary>
        ///     Current Count
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        ///     Last User-Provided Value
        /// </summary>
        public string LastUserValue { get; set; }

        /// <summary>
        ///     Last Value
        /// </summary>
        public double LastValue { get; set; }

        /// <summary>
        ///     Max Value
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        ///     Max User-Provided Value
        /// </summary>
        public string MaxUserValue { get; set; }

        /// <summary>
        ///     Overall Mean
        /// </summary>
        public double Mean { get; set; }

        /// <summary>
        ///     Overall Median
        /// </summary>
        public double Median { get; set; }

        /// <summary>
        ///     Min Value
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        ///     Min User-Provided Value
        /// </summary>
        public string MinUserValue { get; set; }

        /// <summary>
        ///     75th Percentile
        /// </summary>
        public double Percentile75 { get; set; }

        /// <summary>
        ///     95th Percentile
        /// </summary>
        public double Percentile95 { get; set; }

        /// <summary>
        ///     98th Percentile
        /// </summary>
        public double Percentile98 { get; set; }

        /// <summary>
        ///     99th Percentile
        /// </summary>
        public double Percentile99 { get; set; }

        /// <summary>
        ///     99.9th Percentile
        /// </summary>
        public double Percentile999 { get; set; }

        /// <summary>
        ///     Number of Sample-Items
        /// </summary>
        public int SampleSize { get; set; }

        /// <summary>
        ///     Standard Deviation
        /// </summary>
        public double StdDev { get; set; }

        /// <summary>
        ///     Overall Sum
        /// </summary>
        public double Sum { get; set; }
    }

    /// <summary>
    ///     representation of <see cref="MeterValueSource" /> items
    /// </summary>
    public class ServiceMeterMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceMeterMetric(string contextName, MeterValueSource meter)
            : this(contextName, meter.Value, meter.Name)
        {
        }

        /// <inheritdoc />
        public ServiceMeterMetric(string contextName, MeterValue meter, string name)
            : base(contextName, name)
        {
            Count = meter.Count;
            FifteenMinuteRate = meter.FifteenMinuteRate;
            FiveMinuteRate = meter.FiveMinuteRate;
            Items = meter.Items
                         .Select(i => new ServiceMeterSetItem(contextName, i, name))
                         .ToList();
            MeanRate = meter.MeanRate;
            OneMinuteRate = meter.OneMinuteRate;
            RateUnit = meter.RateUnit;
        }

        /// <summary>
        ///     Current Count
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        ///     Rate in the last 15 Minutes
        /// </summary>
        public double FifteenMinuteRate { get; set; }

        /// <summary>
        ///     Rate in the last 5 Minutes
        /// </summary>
        public double FiveMinuteRate { get; set; }

        /// <summary>
        ///     Associated Items
        /// </summary>
        public List<ServiceMeterSetItem> Items { get; set; }

        /// <summary>
        ///     Average Rate
        /// </summary>
        public double MeanRate { get; set; }

        /// <summary>
        ///     Rate in the last Minute
        /// </summary>
        public double OneMinuteRate { get; set; }

        /// <inheritdoc cref="TimeUnit" />
        public TimeUnit RateUnit { get; set; }
    }

    /// <summary>
    ///     specific item in a <see cref="ServiceMeterMetric" />
    /// </summary>
    public class ServiceMeterSetItem
    {
        /// <inheritdoc />
        public ServiceMeterSetItem(string contextName, MeterValue.SetItem item, string name)
        {
            Item = item.Item;
            Percent = item.Percent;
            Tags = item.Tags.ToDictionary();
            Value = new ServiceMeterMetric(contextName, item.Value, name);
        }

        /// <summary>
        ///     Item-Name
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///     Percentage of this Item
        /// </summary>
        public double Percent { get; set; }

        /// <summary>
        ///     Associated Tags
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <inheritdoc cref="ServiceMeterMetric" />
        public ServiceMeterMetric Value { get; set; }
    }

    /// <summary>
    ///     representation of <see cref="TimerValueSource" /> items
    /// </summary>
    public class ServiceTimerMetric : ServiceMetric
    {
        /// <inheritdoc />
        public ServiceTimerMetric(string contextName, TimerValueSource timer)
            : base(contextName, timer.Name)
        {
            ActiveSessions = timer.Value.ActiveSessions;
            Histogram = new ServiceHistogramMetric(contextName, timer.Value.Histogram, timer.Name);
            Rate = new ServiceMeterMetric(contextName, timer.Value.Rate, timer.Name);
            DurationUnit = timer.DurationUnit;
        }

        /// <summary>
        ///     Active Sessions right now
        /// </summary>
        public long ActiveSessions { get; set; }

        /// <inheritdoc6Giraffes!4 cref="TimeUnit" />
        public TimeUnit DurationUnit { get; set; }

        /// <summary>
        ///     Associated Time-History
        /// </summary>
        public ServiceHistogramMetric Histogram { get; set; }

        /// <summary>
        ///     Associated Progress
        /// </summary>
        public ServiceMeterMetric Rate { get; set; }
    }
}