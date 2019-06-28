using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
                      .Build();

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
               .ConfigureAppConfiguration((context, builder) => ConfigureAppConfigurationInternal(builder,
                                                                                                  context.HostingEnvironment.EnvironmentName,
                                                                                                  args))
               .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
               .ConfigureLogging((context, builder) => ConfigureLoggingInternal(builder, context.Configuration));

        /// <summary>
        ///     Configure Application-Configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="environment"></param>
        /// <param name="args"></param>
        private static void ConfigureAppConfigurationInternal(IConfigurationBuilder builder, string environment, string[] args)
            => builder.AddJsonFile("appsettings.json", true, true)
                      .AddJsonFile($"appsettings.{environment}.json", true, true)
                      .AddEnvironmentVariables()
                      .AddEnvironmentVariables("MAV_CONFIG_PROJECTION")
                      .AddCommandLine(args);

        /// <summary>
        ///     Configure Application-Logging
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        private static void ConfigureLoggingInternal(ILoggingBuilder builder, IConfiguration configuration)
        {
            configuration.ConfigureNLog();

            builder.ClearProviders()
                   .SetMinimumLevel(LogLevel.Trace)
                   .AddNLog(configuration.GetSection("LoggingConfiguration"));
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