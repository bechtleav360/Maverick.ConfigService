using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Cli;
using Bechtle.A365.ServiceBase;
using Bechtle.A365.ServiceBase.Commands;
using Bechtle.A365.ServiceBase.Commands.ServiceInstaller;
using Bechtle.A365.ServiceBase.Commands.ServiceInstaller.ServiceManager;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.LayoutRenderers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Main Entry-Point for the Application
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     Activity-Source used for all Activities in this service
        /// </summary>
        public static readonly ActivitySource Source = new("Maverick.ConfigService", "1.0");

        /// <summary>
        ///     Delegate App-Startup to the default ServiceBase-Behaviour
        /// </summary>
        /// <param name="args"></param>
        public static Task<int> Main(string[] args) => InternalMain<Startup, CliBase>(args);

        private static void ConfigureActivityIdLogging()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            LayoutRenderer.Register("Activity.Current.Id", _ => Activity.Current?.Id ?? string.Empty);
            LayoutRenderer.Register("Activity.Current.OperationName", _ => Activity.Current?.OperationName ?? string.Empty);
        }

        private static void ConfigureAppConfiguration(
            HostBuilderContext context,
            IConfigurationBuilder builder,
            string[] args)
        {
            builder.Sources.Clear();

            builder.AddJsonFile("appsettings.json", true, true)
                   .AddJsonFile("appsettings.logging.json", true, true)
                   .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                   .AddEnvironmentVariables()
                   .AddEnvironmentVariables("ASPNETCORE_")
                   .AddEnvironmentVariables("MAV_CONFIG_")
                   .AddCommandLine(args)
                   .AddJsonFile("data/configuration/appsettings.json", true, true);
        }

        private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
        {
            if (context.Configuration is null)
            {
                Console.WriteLine("context does not contain configuration; unable to apply Nlog-Configuration");
                return;
            }

            builder.ClearProviders()
                   .SetMinimumLevel(LogLevel.Trace);

            try
            {
                if (context.Configuration
                           .GetSection("LoggingConfiguration")
                           ?.GetSection("NLog") is { } nLogSection)
                {
                    LogManager.Configuration = new NLogLoggingConfiguration(nLogSection);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"new LoggingConfiguration could not be applied: {e}");
            }

            builder.AddNLog(context.Configuration.GetSection("LoggingConfiguration"));
        }

        /// <summary>
        ///     use the All-in-One Application with a custom Startup
        /// </summary>
        /// <typeparam name="TStartup">Startup-Type, consider extending <see cref="DefaultStartup" /></typeparam>
        /// <param name="args">command-line args</param>
        /// <param name="hostCustomizer">function to customize the prepared <see cref="IHostBuilder" /> for your application</param>
        private static void ServiceMain<TStartup>(string[] args, Action<IHostBuilder>? hostCustomizer = null)
            where TStartup : class
        {
            ConfigureActivityIdLogging();

            IHostBuilder host = Host.CreateDefaultBuilder(args)
                                    .ConfigureAppConfiguration((context, builder) => ConfigureAppConfiguration(context, builder, args))
                                    .ConfigureLogging(ConfigureLogging)
                                    .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); });

            hostCustomizer?.Invoke(host);

            try
            {
                host.Build().Run();
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        /// <summary>
        ///     use the All-in-One Application with cli integration and a custom Startup
        /// </summary>
        /// <param name="args">command-line args</param>
        /// <param name="cliHostCustomizer">function to customize <see cref="IHostBuilder" /> for the cli application</param>
        /// <param name="appHostCustomizer">function to customize the prepared <see cref="IHostBuilder" /> for your application</param>
        /// <typeparam name="TStartup">Startup-Type, consider extending <see cref="DefaultStartup" /></typeparam>
        /// <typeparam name="TCommandRoot">CommandRoot-Type, as the entrypoint for the cli</typeparam>
        /// <returns></returns>
        private static async Task<int> InternalMain<TStartup, TCommandRoot>(
            string[] args,
            Action<IHostBuilder>? cliHostCustomizer = null,
            Action<IHostBuilder>? appHostCustomizer = null)
            where TStartup : class
            where TCommandRoot : class
        {
            ConfigureActivityIdLogging();
            IHostBuilder commandLineHost = new HostBuilder()
                                           .ConfigureAppConfiguration((context, builder) => ConfigureAppConfiguration(context, builder, args))
                                           .ConfigureLogging(ConfigureLogging)
                                           .ConfigureServices(
                                               services =>
                                               {
                                                   services
                                                       .AddLogging()
                                                       .AddSingleton(
                                                           p =>
                                                               p.GetService<IConfiguration>()
                                                                ?.GetSection("ServiceConfig")
                                                                .Get<ServiceConfig>()
                                                               ?? new ServiceConfig())
                                                       .AddSingleton(PhysicalConsole.Singleton)
                                                       .AddSingleton<IOutput, Output>();

                                                   if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                                   {
                                                       services.AddSingleton<IServiceManager, WindowsServiceManager>();
                                                   }
                                                   else
                                                   {
                                                       services.AddSingleton<IServiceManager, NotSupportedServiceManager>();
                                                   }
                                               });

            cliHostCustomizer?.Invoke(commandLineHost);

            return await commandLineHost.RunCommandLineApplicationAsync<TCommandRoot>(
                       args,
                       application =>
                       {
                           application.OnExecute(
                               () =>
                               {
                                   try
                                   {
                                       ServiceMain<TStartup>(args, appHostCustomizer);
                                       return 0;
                                   }
                                   catch (Exception ex)
                                   {
                                       LogManager.GetLogger("Setup").Fatal(ex);
                                       return -1;
                                   }
                                   finally
                                   {
                                       LogManager.Shutdown();
                                   }
                               });
                       });
        }
    }
}
