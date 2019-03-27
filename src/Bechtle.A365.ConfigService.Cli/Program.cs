﻿using Bechtle.A365.ConfigService.Cli.Commands;
using Bechtle.A365.ConfigService.Common.DbObjects;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bechtle.A365.ConfigService.Cli
{
    [Subcommand(
        typeof(ExportCommand),
        typeof(ImportCommand),
        typeof(TestCommand),
        typeof(MigrateCommand))]
    public class Program
    {
        [Option("-c|--connection-string", CommandOptionType.SingleValue, Description = "ConnectionString to use for Connecting to the Database")]
        public string ConnectionString { get; set; }

        public static int Main(string[] args)
        {
            var services = new ServiceCollection()
                           .AddSingleton(PhysicalConsole.Singleton)
                           .AddSingleton<ApplicationSettings>()
                           .AddDbContext<ProjectionStoreContext>(
                               (provider, builder) => builder.UseSqlServer(provider.GetService<ApplicationSettings>().ConnectionString,
                                                                           o => o.MigrationsAssembly("Bechtle.A365.ConfigService.Migrations")))
                           .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
               .UseDefaultConventions()
               .UseConstructorInjection(services);

            return app.Execute(args);
        }

        // ReSharper disable once UnusedMember.Local
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }

    public class ApplicationSettings
    {
        public string ConnectionString { get; set; }
    }
}