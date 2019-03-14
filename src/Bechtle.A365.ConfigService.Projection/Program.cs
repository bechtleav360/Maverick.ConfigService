using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace Bechtle.A365.ConfigService.Projection
{
    public class Program
    {
        // entry point for application when designing migrations
        // ReSharper disable once UnusedMember.Global
        //
        // Configure and ConfigureServices are both required, either as Fluent invocations here or as actual methods in this class - even if they're empty.
        // they're both called here to not clutter this class with empty functions
        public static IWebHost BuildWebHost(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                      .Configure(builder => { }) // don't delete, see comment above
                      .ConfigureAppConfiguration((context, builder) =>
                      {
                          builder.AddJsonFile("appsettings.json", true, true)
                                 .AddCommandLine(args)
                                 .AddEnvironmentVariables();
                      })
                      .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
                      .ConfigureLogging((context, builder) =>
                      {
                          // set the global NLog configuration
                          using (var stringReader = new StringReader(context.Configuration
                                                                            .Get<ProjectionConfiguration>()
                                                                            .LoggingConfiguration))
                          using (var xmlReader = XmlReader.Create(stringReader))
                              LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);

                          builder.ClearProviders();
                      })
                      .UseNLog()
                      .Build();

        private static IHostBuilder BuildHost(string[] args)
            => new HostBuilder()
               .ConfigureAppConfiguration((context, builder) =>
               {
                   builder.AddJsonFile("appsettings.json", true, true)
                          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                          .AddCommandLine(args)
                          .AddEnvironmentVariables();
               })
               .ConfigureServices((context, services) => ConfigureServicesInternal(services, context.Configuration))
               .ConfigureLogging((context, builder) =>
               {
                   // set the global NLog configuration
                   using (var stringReader = new StringReader(context.Configuration
                                                                     .Get<ProjectionConfiguration>()
                                                                     .LoggingConfiguration))
                   using (var xmlReader = XmlReader.Create(stringReader))
                       LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);

                   builder.ClearProviders();
               })
               .UseNLog();

        // actual entry-point for the application
        public static async Task Main(string[] args)
        {
            var host = BuildHost(args);

            if (!(Debugger.IsAttached || args.Any(a => a == "--console")))
                await host.RunAsServiceAsync();
            else
                await host.RunConsoleAsync();
        }

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