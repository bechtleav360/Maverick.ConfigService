using System;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.EventFactories;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Logging.NLog.Extension;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ESLogger = EventStore.ClientAPI.ILogger;

namespace Bechtle.A365.ConfigService.Projection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     add logging for ourselves, and EventStore
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCustomLogging(this IServiceCollection services)
            => services.AddSingleton(provider =>
                       {
                           ILoggerFactory factory = new LoggerFactory();
                           factory.AddA365NlogProviderWithConfiguration(provider.GetService<ProjectionConfiguration>()
                                                                                .LoggingConfiguration);

                           return factory;
                       })
                       .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                       .AddSingleton<ESLogger, EventStoreLogger>();

        /// <summary>
        ///     add services to handle DomainEvent tasks
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDomainEventServices(this IServiceCollection services)
            => services
               // add DomainEventSerializer as generic class for IDomainEventSerializer
               .AddSingleton(typeof(IDomainEventSerializer<>), typeof(DomainEventSerializer<>))
               // register all IDomainEventHandlers
               // IMPORTANT: this needs to be updated once new events are added
               .AddSingleton<IDomainEventHandler<DefaultEnvironmentCreated>, DefaultEnvironmentCreatedHandler>()
               .AddSingleton<IDomainEventHandler<EnvironmentCreated>, EnvironmentCreatedHandler>()
               .AddSingleton<IDomainEventHandler<EnvironmentDeleted>, EnvironmentDeletedHandler>()
               .AddSingleton<IDomainEventHandler<EnvironmentKeysModified>, EnvironmentKeyModifiedHandler>()
               .AddSingleton<IDomainEventHandler<StructureCreated>, StructureCreatedHandler>()
               .AddSingleton<IDomainEventHandler<StructureDeleted>, StructureDeletedHandler>()
               .AddSingleton<IDomainEventHandler<ConfigurationBuilt>, ConfigurationBuiltHandler>();

        /// <summary>
        ///     add configuration as a whole, and parts of it
        /// </summary>
        /// <param name="services"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionConfiguration(this IServiceCollection services, HostBuilderContext context)
            => services.AddSingleton(context.Configuration)
                       .AddSingleton(provider => provider.GetService<IConfiguration>().Get<ProjectionConfiguration>())
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().EventBus)
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().EventStore)
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().Storage);

        /// <summary>
        ///     services for specific tasks within the projection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionServices(this IServiceCollection services)
            => services.AddSingleton(provider =>
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
                       .AddSingleton<IEventDeserializer, EventDeserializer>()
                       .AddSingleton<IConfigurationParser, ConfigurationParser>()
                       .AddSingleton<IConfigurationCompiler, ConfigurationCompiler>()
                       .AddSingleton<IConfigurationDatabase, ConfigurationDatabase>()
                       .AddSingleton<IJsonTranslator, JsonTranslator>()
                       .AddSingleton<IEventBus, WebSocketEventBusClient>(provider =>
                       {
                           var config = provider.GetService<ProjectionEventBusConfiguration>();
                           var loggerFactory = provider.GetService<ILoggerFactory>();

                           var client = new WebSocketEventBusClient(new Uri(new Uri(config.Server), config.Hub).ToString(), 
                                                                    loggerFactory);

                           client.Connect().Wait();

                           return client;
                       });
    }
}