﻿using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bechtle.A365.ConfigService.Migrator
{
    public static class Program
    {
        private static IHostBuilder BuildHost(string[] args)
            => new HostBuilder()
               .ConfigureAppConfiguration((context, builder) =>
               {
                   builder.AddJsonFile("appsettings.json", true, false)
                          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, false)
                          .AddCommandLine(args)
                          .AddEnvironmentVariables();
               })
               .ConfigureServices(
                   (context, services) => services.AddDbContext<ProjectionStoreContext>(
                                                      (provider, builder) => builder.UseSqlServer(
                                                          context.Configuration["ConnectionString"],
                                                          options => options.MigrationsAssembly(typeof(ProjectionStoreContext).Assembly.FullName)))
                                                  .AddHostedService<DatabaseMigrationExecutor<ProjectionStoreContext>>());

        // actual entry-point for the application
        public static async Task Main(string[] args)
        {
            var host = BuildHost(args);

            await host.RunConsoleAsync();
        }
    }
}