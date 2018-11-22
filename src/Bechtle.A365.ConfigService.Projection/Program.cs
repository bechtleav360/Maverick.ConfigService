using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Program
    {
        // entry point for application when designing migrations
        // ReSharper disable once UnusedMember.Global
        public static IWebHost BuildWebHost(string[] args) => WebHost.CreateDefaultBuilder(args)
                                                                     .Configure(builder => { })
                                                                     .ConfigureServices(
                                                                         (context, collection) => ConfigureServicesInternal(collection,
                                                                                                                            context.Configuration))
                                                                     .ConfigureAppConfiguration(builder =>
                                                                     {
                                                                         builder.AddJsonFile("appsettings.json", true, true)
                                                                                .AddCommandLine(args)
                                                                                .AddEnvironmentVariables();
                                                                     })
                                                                     .Build();

        // actual entry-point for the application
        public static async Task Main(string[] args)
            => await new HostBuilder()
                     .ConfigureAppConfiguration(builder =>
                     {
                         builder.AddJsonFile("appsettings.json", true, true)
                                .AddCommandLine(args)
                                .AddEnvironmentVariables();
                     })
                     .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
                     .RunConsoleAsync();

        private static void ConfigureServicesInternal(IServiceCollection services, IConfiguration configuration)
            => services.AddDbContext<ProjectionStore>(
                           (provider, builder) => builder.UseLazyLoadingProxies()
                                                         .UseLoggerFactory(new NullLoggerFactory())
                                                         .UseSqlServer(provider.GetService<ProjectionStorageConfiguration>()
                                                                               .ConnectionString))

                       // required for lazy-loading proxies
                       .AddEntityFrameworkProxies()
                       .AddCustomLogging()
                       .AddProjectionConfiguration(configuration)
                       .AddProjectionServices()
                       .AddDomainEventServices()

                       // add the service that should be run
                       .AddHostedService<Projection>();
    }
}