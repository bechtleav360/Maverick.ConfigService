using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Bechtle.A365.ConfigService.Projection.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog.Extensions.Logging;
using RabbitMQ.Client;

namespace Bechtle.A365.ConfigService.Projection
{
    public static class Program
    {
        public static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        ///     actual entry-point for this Application
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var host = BuildHost(args);

                if (!(Debugger.IsAttached || args.Any(a => a == "--console")))
                    await host.RunAsServiceAsync(CancellationTokenSource.Token);
                else
                    await host.RunConsoleAsync(CancellationTokenSource.Token);

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"uncaught error during application-runtime: \r\n{e}");
                return e.HResult;
            }
        }

        /// <summary>
        ///     Build the Actual host that is used to run this Application.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IHostBuilder BuildHost(string[] args)
            => new HostBuilder()
               .ConfigureHostConfiguration(builder => builder.AddEnvironmentVariables("ASPNETCORE_"))
               .ConfigureAppConfiguration((context, builder) => ConfigureAppConfigurationInternal(builder, context, args))
               .ConfigureServices((context, services) => ConfigureServicesInternal(services, context))
               .ConfigureLogging((context, builder) => ConfigureLoggingInternal(builder, context));

        private static void ConfigureMetrics(HostBuilderContext context, IMetricsBuilder builder)
        {
            var options = context.Configuration.GetSection("MetricsReporting").Get<RabbitMetricsReporterOptions>();

            if (options.Enabled)
                builder.Report.Using(new RabbitMetricsReporter(options));
        }

        /// <summary>
        ///     Configure Application-Configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        /// <param name="args"></param>
        private static void ConfigureAppConfigurationInternal(IConfigurationBuilder builder, HostBuilderContext context, string[] args)
            => builder.AddJsonFile("appsettings.json", true, true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                      .AddEnvironmentVariables()
                      .AddEnvironmentVariables("MAV_CONFIG_PROJECTION")
                      .AddCommandLine(args);

        /// <summary>
        ///     Configure Application-Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        private static void ConfigureLoggingInternal(ILoggingBuilder builder, HostBuilderContext context)
        {
            context.Configuration.ConfigureNLog();

            builder.ClearProviders()
                   .SetMinimumLevel(LogLevel.Trace)
                   .AddNLog(context.Configuration.GetSection("LoggingConfiguration"));
        }

        /// <summary>
        ///     Configure DI-Services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="context"></param>
        private static void ConfigureServicesInternal(IServiceCollection services, HostBuilderContext context)
        {
            var logger = services.BuildServiceProvider()
                                 .GetService<ILoggerFactory>()
                                 ?.CreateLogger(nameof(Program));

            services.AddDbContext<ProjectionStoreContext>(
                        logger, (provider, builder) =>
                        {
                            var settings = provider.GetService<ProjectionStorageConfiguration>();

                            // @IMPORTANT: when handling additional cases here, don't forget to update the error-messages
                            switch (settings.Backend)
                            {
                                case DbBackend.MsSql:
                                    logger.LogDebug("using MsSql database-backend");
                                    builder.UseSqlServer(settings.ConnectionString);
                                    break;

                                case DbBackend.Postgres:
                                    logger.LogDebug("using PostgreSql database-backend");
                                    builder.UseNpgsql(settings.ConnectionString);
                                    break;

                                case DbBackend.None:
                                default:
                                    logger.LogError($"Unsupported DbBackend: '{settings.Backend}'; " +
                                                    $"change ProjectionStorage:Backend; " +
                                                    $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");
                                    throw new ArgumentOutOfRangeException(nameof(settings.Backend),
                                                                          $"Unsupported DbBackend: '{settings.Backend}'; " +
                                                                          $"change ProjectionStorage:Backend; " +
                                                                          $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");
                            }
                        })
                    .AddCustomLogging(logger)
                    .AddProjectionConfiguration(logger, context.Configuration)
                    .AddProjectionServices(logger)
                    .AddDomainEventServices(logger)
                    .AddSingleton<IMetricService, ProjectionMetricService>(logger)
                    // register metrics-services
                    .AddSingleton<RabbitMetricsReporter>()
                    .AddMetrics(builder => ConfigureMetrics(context, builder))
                    .AddMetricsReportingHostedService()
                    // add the service that should be run
                    .AddHostedService<StatusReporter>(logger)
                    .AddSingleton<IEventQueue, EventQueue>(logger)
                    .AddHostedService<EventConverter>(logger)
                    .AddHostedService<EventProjection>(logger);
        }
    }

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

    public class RabbitMetricsReporterOptions
    {
        public string AppId { get; set; }

        public bool Enabled { get; set; }

        public string Exchange { get; set; }

        public TimeSpan FlushInterval { get; set; }

        public string Hostname { get; set; }

        public string Password { get; set; }

        public int Port { get; set; }

        public string Topic { get; set; }

        public string Username { get; set; }
    }
}