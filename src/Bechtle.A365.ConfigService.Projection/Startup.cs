using System;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
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
                    .AddSingleton<IProjection, Projection>()
                    .AddSingleton<IConfigurationCompiler, ConfigurationCompiler>()
                    .AddSingleton<IConfigurationDatabase, InMemoryConfigurationDatabase>()

                    // add DomainEventSerializer as generic class for IDomainEventSerializer
                    .AddSingleton(typeof(IDomainEventSerializer<>), typeof(DomainEventSerializer<>))

                    // register all IDomainEventHandlers
                    // IMPORTANT: this needs to be updated once new events are added
                    .AddSingleton<IDomainEventHandler<EnvironmentCreated>, EnvironmentCreatedHandler>()
                    .AddSingleton<IDomainEventHandler<EnvironmentDeleted>, EnvironmentDeletedHandler>()
                    .AddSingleton<IDomainEventHandler<EnvironmentKeyModified>, EnvironmentKeyModifiedHandler>()
                    .AddSingleton<IDomainEventHandler<StructureCreated>, StructureCreatedHandler>()
                    .AddSingleton<IDomainEventHandler<StructureDeleted>, StructureDeletedHandler>()
                    .AddSingleton<IDomainEventHandler<ConfigurationBuilt>, ConfigurationBuiltHandler>();
        }
    }
}