using System;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.Compilation;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.Logging.NLog.Extension;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                // required for lazy-loading proxies
                .AddEntityFrameworkProxies()

                // logging for ourselves, and EventStore
                .AddSingleton(provider =>
                {
                    ILoggerFactory factory = new LoggerFactory();
                    factory.AddA365NlogProviderWithConfiguration(provider.GetService<ProjectionConfiguration>()
                                                                         .LoggingConfiguration);

                    return factory;
                })
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddSingleton<ESLogger, EventStoreLogger>()

                // configuration as a whole, and parts of it
                .AddSingleton(Configuration)
                .AddSingleton(provider => provider.GetService<IConfiguration>().Get<ProjectionConfiguration>())
                .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().EventStore)
                .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().Storage)

                // services for specific tasks
                .AddSingleton(provider =>
                {
                    var config = provider.GetService<ProjectionConfiguration>()
                                 ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));

                    return EventStoreConnection.Create(ConnectionSettings.Create()
                                                                         .KeepReconnecting()
                                                                         .KeepRetrying()
                                                                         .UseCustomLogger(provider.GetService<ESLogger>()),
                                                       new Uri(config.EventStore.Uri),
                                                       config.EventStore.ConnectionName);
                })
                .AddSingleton<IProjection, Projection>()
                .AddSingleton<IEventDeserializer, EventDeserializer>()
                .AddSingleton<IConfigurationParser, ConfigurationParser>()
                .AddSingleton<IConfigurationCompiler, ConfigurationCompiler>()
                .AddSingleton<IConfigurationDatabase, ConfigurationDatabase>()

                // add DomainEventSerializer as generic class for IDomainEventSerializer
                .AddSingleton(typeof(IDomainEventSerializer<>), typeof(DomainEventSerializer<>))

                // register all IDomainEventHandlers
                // IMPORTANT: this needs to be updated once new events are added
                .AddSingleton<IDomainEventHandler<EnvironmentCreated>, EnvironmentCreatedHandler>()
                .AddSingleton<IDomainEventHandler<EnvironmentDeleted>, EnvironmentDeletedHandler>()
                .AddSingleton<IDomainEventHandler<EnvironmentKeysModified>, EnvironmentKeyModifiedHandler>()
                .AddSingleton<IDomainEventHandler<StructureCreated>, StructureCreatedHandler>()
                .AddSingleton<IDomainEventHandler<StructureDeleted>, StructureDeletedHandler>()
                .AddSingleton<IDomainEventHandler<ConfigurationBuilt>, ConfigurationBuiltHandler>();
        }
    }
}