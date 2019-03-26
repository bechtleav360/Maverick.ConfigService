﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Web;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Projection
{
    public static class Program
    {
        public static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        // ReSharper disable once UnusedMember.Global
        //
        // Configure and ConfigureServices are both required, either as Fluent invocations here or as actual methods in this class - even if they're empty.
        // they're both called here to not clutter this class with empty functions
        /// <summary>
        ///     Mock Entry-Point for building / managing Migrations
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHost BuildWebHost(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                      .Configure(builder => { }) // don't delete, see comment above
                      .ConfigureAppConfiguration((context, builder) => ConfigureAppConfigurationInternal(builder,
                                                                                                         context.HostingEnvironment.EnvironmentName,
                                                                                                         args))
                      .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
                      .ConfigureLogging((context, builder) => ConfigureLoggingInternal(builder, context.Configuration))
                      .UseNLog()
                      .Build();

        public static void ConfigureNLog(string configuration, ILogger logger = null)
        {
            try
            {
                logger?.LogInformation("Configuration has been reloaded - applying LoggingConfiguration");

                if (string.IsNullOrWhiteSpace(configuration))
                {
                    logger?.LogWarning("no Logging-Configuration found at 'LoggingConfiguration' - skipping re-configuration");
                    return;
                }

                using (var stringReader = new StringReader(configuration))
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);
                }

                logger?.LogInformation("new LoggingConfiguration has been applied");
            }
            catch (Exception e)
            {
                logger?.LogWarning($"new LoggingConfiguration could not be applied: {e}");
            }
        }

        /// <summary>
        ///     actual entry-point for this Application
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task<int> Main(string[] args)
        {
            IHostBuilder host;

            try
            {
                host = BuildHost(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"error while building application-host: {e}");
                return e.HResult;
            }

            try
            {
                if (!(Debugger.IsAttached || args.Any(a => a == "--console")))
                    await host.RunAsServiceAsync(CancellationTokenSource.Token);
                else
                    await host.RunConsoleAsync(CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"error while running application: {e}");
                return e.HResult;
            }

            return 0;
        }

        /// <summary>
        ///     Build the Actual host that is used to run this Application.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IHostBuilder BuildHost(string[] args)
            => new HostBuilder()
               .ConfigureAppConfiguration((context, builder) => ConfigureAppConfigurationInternal(builder,
                                                                                                  context.HostingEnvironment.EnvironmentName,
                                                                                                  args))
               .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
               .ConfigureLogging((context, builder) => ConfigureLoggingInternal(builder, context.Configuration))
               .UseNLog();

        /// <summary>
        ///     Configure Application-Configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="environment"></param>
        /// <param name="args"></param>
        private static void ConfigureAppConfigurationInternal(IConfigurationBuilder builder, string environment, string[] args)
            => builder.AddJsonFile("appsettings.json", true, true)
                      .AddJsonFile($"appsettings.{environment}.json", true, true)
                      .AddCommandLine(args)
                      .AddEnvironmentVariables();

        /// <summary>
        ///     Configure Application-Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        private static void ConfigureLoggingInternal(ILoggingBuilder builder, IConfiguration configuration)
        {
            ConfigureNLog(configuration.Get<ProjectionConfiguration>().LoggingConfiguration);

            builder.ClearProviders();
        }

        /// <summary>
        ///     Configure DI-Services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        private static void ConfigureServicesInternal(IServiceCollection services, IConfiguration configuration)
            => services.AddDbContext<ProjectionStoreContext>(
                           (provider, builder)
                               => builder.UseSqlServer(provider.GetService<ProjectionStorageConfiguration>().ConnectionString,
                                                       options => options.MigrationsAssembly(typeof(ProjectionStoreContext).Assembly.FullName)))
                       .AddCustomLogging()
                       .AddProjectionConfiguration(configuration)
                       .AddProjectionServices()
                       .AddDomainEventServices()
                       // add the service that should be run
                       .AddHostedService<Projection>();
    }
}