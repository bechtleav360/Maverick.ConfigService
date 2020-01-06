using Bechtle.A365.ConfigService.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bechtle.A365.ConfigService.Cli
{
    [Subcommand(
        typeof(BrowseCommand),
        typeof(CompareCommand),
        typeof(ExportCommand),
        typeof(ImportCommand),
        typeof(TestCommand))]
    public class Program
    {
        public static int Main(string[] args)
        {
            var services = ConfigureServicesInternal().BuildServiceProvider();

            using var app = new CommandLineApplication<Program>();

            app.Conventions
               .UseDefaultConventions()
               .UseConstructorInjection(services);

            return app.Execute(args);
        }

        private static IServiceCollection ConfigureServicesInternal(IServiceCollection services = null)
        {
            services ??= new ServiceCollection();

            services.AddSingleton(PhysicalConsole.Singleton)
                    // override how we get our ApplicationSettings to inject IConfiguration-data
                    .AddSingleton(p => p.GetService<IConfiguration>()?.Get<ApplicationSettings>() ?? new ApplicationSettings())
                    .AddSingleton<IOutput, Output>();

            return services;
        }

        // ReSharper disable once UnusedMember.Global
        //
        // Configure(HostConfiguration) and ConfigureServices are both required, either as Fluent invocations here or as actual methods in this class - even if they're empty.
        // they're both called here to not clutter this class with empty functions
        /// <summary>
        ///     Mock Entry-Point for building / managing Migrations
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHost BuildWebHost(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration((context, builder) =>
                   {
                       builder.Sources.Clear();
                       builder.AddJsonFile("appsettings.migrations.json", false, false);
                   })
                   // required, see comment above
                   .ConfigureHostConfiguration(builder => { })
                   .ConfigureServices(services => ConfigureServicesInternal(services))
                   .Build();

        // ReSharper disable once UnusedMember.Local
        /// <summary>
        ///     this method is executed when no args were given
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }
}