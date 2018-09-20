using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(provider => new LoggerFactory().AddConsole(LogLevel.Trace))
                    .AddSingleton(Configuration)
                    .AddSingleton(provider => provider.GetService<IConfiguration>()
                                                      .Get<ProjectionConfiguration>())
                    .AddSingleton(provider =>
                    {
                        var config = provider.GetService<ProjectionConfiguration>()
                                     ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));

                        return EventStoreConnection.Create(new Uri(config.EventStoreUri), config.ConnectionName);
                    })
                    .AddSingleton<IConfigurationDatabase, InMemoryConfigurationDatabase>()
                    .AddSingleton<IProjection, Projection>();
        }
    }
}