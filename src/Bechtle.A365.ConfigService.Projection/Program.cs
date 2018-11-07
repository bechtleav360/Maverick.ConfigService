using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Bechtle.A365.ConfigService.Projection
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await new HostBuilder()
                     .ConfigureAppConfiguration(builder =>
                     {
                         builder.AddJsonFile("appsettings.json", true, true)
                                .AddCommandLine(args)
                                .AddEnvironmentVariables();
                     })
                     .ConfigureServices((context, services) =>
                     {
                         services
                             // required for lazy-loading proxies
                             .AddEntityFrameworkProxies()
                             .AddCustomLogging()
                             .AddProjectionConfiguration(context)
                             .AddProjectionServices()
                             .AddDomainEventServices()

                             // add the service that should be run
                             .AddHostedService<Projection>();
                     })
                     .RunConsoleAsync();
    }
}