using System;
using Bechtle.A365.ConfigService.Cli.Commands;
using Bechtle.A365.ConfigService.Common.DbObjects;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bechtle.A365.ConfigService.Cli
{
    [Subcommand(
        typeof(BrowseCommand),
        typeof(CompareCommand),
        typeof(ExportCommand),
        typeof(ImportCommand),
        typeof(TestCommand),
        typeof(MigrateCommand))]
    public class Program
    {
        public static int Main(string[] args)
        {
            var services = ConfigureServicesInternal().BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
               .UseDefaultConventions()
               .UseConstructorInjection(services);

            return app.Execute(args);
        }

        private static IServiceCollection ConfigureServicesInternal(IServiceCollection services = null)
        {
            services = services ?? new ServiceCollection();

            services.AddSingleton(PhysicalConsole.Singleton)
                    // override how we get our ApplicationSettings to inject IConfiguration-data
                    .AddSingleton(p => p.GetService<IConfiguration>()?.Get<ApplicationSettings>() ?? new ApplicationSettings())
                    .AddSingleton<IOutput, Output>()
                    .AddDbContext<ProjectionStoreContext>(
                        (provider, builder) =>
                        {
                            var appsettings = provider.GetService<ApplicationSettings>();

                            switch (appsettings.DbType)
                            {
                                case DbBackend.MsSql:
                                    builder.UseSqlServer(appsettings.ConnectionString,
                                                         o => o.MigrationsAssembly("Bechtle.A365.ConfigService.Migrations"));
                                    break;

                                case DbBackend.Postgres:
                                    builder.UseNpgsql(appsettings.ConnectionString,
                                                      o => o.MigrationsAssembly("Bechtle.A365.ConfigService.Migrations"));
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        });

            return services;
        }

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
                      .ConfigureAppConfiguration((context, builder) =>
                      {
                          builder.Sources.Clear();
                          builder.AddJsonFile("appsettings.migrations.json", false, false);
                      })
                      // required, see comment above
                      .Configure(builder => { })
                      .ConfigureServices(services => ConfigureServicesInternal(services))
                      .Build();

        // ReSharper disable once UnusedMember.Local
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }
}