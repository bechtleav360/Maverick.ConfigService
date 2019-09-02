using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Filters;
using App.Metrics.Formatters;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using App.Metrics.Reporting;
using App.Metrics.Timer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RabbitMQ.Client;

namespace Bechtle.A365.ConfigService.Projection.Metrics
{
    /// <inheritdoc cref="IReportMetrics" />
    public class RabbitMetricsReporter : IReportMetrics, IDisposable
    {
        private readonly RabbitMetricsReporterOptions _options;
        private readonly ConnectionFactory _connectionFactory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private IModel _rabbitConnection;

        public RabbitMetricsReporter(RabbitMetricsReporterOptions options)
        {
            _options = options;
            FlushInterval = _options?.FlushInterval ?? TimeSpan.FromSeconds(30);

            _connectionFactory = new ConnectionFactory
            {
                UserName = _options?.Username ?? string.Empty,
                Password = _options?.Password ?? string.Empty,
                HostName = _options?.Hostname ?? string.Empty,
                Port = _options?.Port ?? 5601
            };

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                Converters =
                {
                    new IsoDateTimeConverter(),
                    new StringEnumConverter()
                },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                FloatFormatHandling = FloatFormatHandling.DefaultValue
            };
        }

        private void Connect()
        {
            try
            {
                if (!(_rabbitConnection is null) && !_rabbitConnection.IsClosed)
                    return;

                var connection = _connectionFactory.CreateConnection();
                _rabbitConnection = connection.CreateModel();
            }
            catch (Exception e)
            {
            }
        }

        /// <inheritdoc />
        public Task<bool> FlushAsync(MetricsDataValueSource metricsData, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                Connect();

                if (_rabbitConnection is null
                    || _rabbitConnection.IsClosed
                    || cancellationToken.IsCancellationRequested)
                    return Task.FromResult(false);

                // select all available metrics from all categories into a tuple with its context
                // (Context, {Metric}ValueSource)
                // ----
                // for each context do:
                //     collect all sources into an array
                //     enumerate each source
                //     take each list of metrics and select it into a flat list
                //     pair each metric with current source
                var metricsSelector = metricsData.Contexts
                                                 .SelectMany(source => new IEnumerable<object>[]
                                                                       {
                                                                           source.ApdexScores,
                                                                           source.Counters,
                                                                           source.Gauges,
                                                                           source.Histograms,
                                                                           source.Meters,
                                                                           source.Timers
                                                                       }
                                                                       .SelectMany(_ => _)
                                                                       .Select(_ => (source, _)));

                foreach (var (context, metric) in metricsSelector)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return Task.FromResult(false);

                    string name;
                    object simpleMetric;

                    switch (metric)
                    {
                        case ApdexValueSource apdex:
                            name = apdex.Name;
                            simpleMetric = TransformApdex(context.Context, apdex);
                            break;

                        case CounterValueSource counter:
                            name = counter.Name;
                            simpleMetric = TransformCounter(context.Context, counter);
                            break;

                        case GaugeValueSource gauge:
                            name = gauge.Name;
                            simpleMetric = TransformGauge(context.Context, gauge);
                            break;

                        case HistogramValueSource histogram:
                            name = histogram.Name;
                            simpleMetric = TransformHistogram(context.Context,
                                                              histogram.Value,
                                                              histogram.Name,
                                                              histogram.Tags.ToDictionary());
                            break;

                        case MeterValueSource meter:
                            name = meter.Name;
                            simpleMetric = TransformMeterMetric(context.Context,
                                                                meter.Value,
                                                                meter.Name,
                                                                meter.Tags.ToDictionary());
                            break;

                        case TimerValueSource timer:
                            name = timer.Name;
                            simpleMetric = TransformTimer(context.Context, timer);
                            break;

                        default:
                            continue;
                    }

                    var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(simpleMetric, _jsonSerializerSettings));

                    _rabbitConnection.BasicPublish(_options.Exchange,
                                                   string.Format(_options.Topic, context.Context, name),
                                                   body: message);
                }

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        public IFilterMetrics Filter { get; set; }

        /// <inheritdoc />
        public TimeSpan FlushInterval { get; set; }

        /// <inheritdoc />
        public IMetricsOutputFormatter Formatter { get; set; }

        public void Dispose()
        {
            if (_rabbitConnection?.IsOpen == true)
                _rabbitConnection?.Dispose();
        }

        private static object TransformApdex(string context, ApdexValueSource apdex) => new
        {
            UtcTime = DateTime.UtcNow.ToString("O"),
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
            UtcTime = DateTime.UtcNow.ToString("O"),
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
            UtcTime = DateTime.UtcNow.ToString("O"),
            ContextName = context,
            Name = gauge.Name,
            Value = gauge.Value,
            Tags = gauge.Tags.ToDictionary()
        };

        private static object TransformHistogram(string context, HistogramValue histogram, string name, IDictionary<string, string> tags = null) => new
        {
            UtcTime = DateTime.UtcNow.ToString("O"),
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
            UtcTime = DateTime.UtcNow.ToString("O"),
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
            UtcTime = DateTime.UtcNow.ToString("O"),
            ContextName = context,
            Name = name,
            Item = item.Item,
            Percent = item.Percent,
            Tags = item.Tags.ToDictionary(),
            Value = TransformMeterMetric(context, item.Value, name)
        };

        private static object TransformTimer(string context, TimerValueSource timer) => new
        {
            UtcTime = DateTime.UtcNow.ToString("O"),
            ContextName = context,
            Name = timer.Name,
            ActiveSessions = timer.Value.ActiveSessions,
            Histogram = TransformHistogram(context, timer.Value.Histogram, timer.Name),
            Rate = TransformMeterMetric(context, timer.Value.Rate, timer.Name),
            DurationUnit = timer.DurationUnit,
            Tags = timer.Tags.ToDictionary()
        };
    }
}