﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Bechtle.A365.ConfigService.Projection.Metrics;
using Bechtle.A365.ConfigService.Projection.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

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
                                    logger.LogError("no DbBackend chosen; change ProjectionStorage:Backend; " +
                                                    $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");
                                    throw new ArgumentOutOfRangeException(nameof(settings.Backend),
                                                                          "no DbBackend chosen; change ProjectionStorage:Backend; " +
                                                                          $"set either {DbBackend.MsSql:G} or {DbBackend.Postgres:G} as Db-Backend");

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
}