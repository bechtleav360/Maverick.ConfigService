using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Program
    {
        public static void Main(string[] args) => MainAsync(args).RunSync();

        public async static Task MainAsync(string[] args)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            ConfigureAppConfiguration(configurationBuilder, args);

            IServiceCollection services = new ServiceCollection();

            var startup = new Startup(configurationBuilder.Build());

            startup.ConfigureServices(services);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetService<ILoggerFactory>()
                           .AddConsole(LogLevel.Debug);

            var logger = serviceProvider.GetService<ILoggerFactory>()
                                        .CreateLogger<Program>();

            logger.LogDebug("program initialized");

            var projection = serviceProvider.GetService<IProjection>();

            var cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) => cancellationTokenSource.Cancel();

            // i want to execute IProjection.Start, but allow graceful termination of this program
            // not sure if Task.Run(..., CancellationToken) handles this, or IProjection.Start(CancellationToken) is requried
            // @TODO: investigato!
            await Task.Run(() => projection.Start(cancellationTokenSource.Token), cancellationTokenSource.Token);

            logger.LogDebug("program shutdown gracefully");
        }

        private static void ConfigureAppConfiguration(IConfigurationBuilder builder, string[] args)
        {
            builder.AddJsonFile("appsettings.json", true, true)
                   .AddCommandLine(args)
                   .AddEnvironmentVariables();
        }
    }
}