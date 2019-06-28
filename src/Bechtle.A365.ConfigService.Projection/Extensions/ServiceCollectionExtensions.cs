using System;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            => services.AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                       .AddSingleton<ESLogger, EventStoreLogger>();

        /// <summary>
        ///     add services to handle DomainEvent tasks
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDomainEventServices(this IServiceCollection services)
            => services
               // add DomainEventConverter as generic class for IDomainEventConverter
               .AddSingleton(typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>))
               // register all IDomainEventHandlers
               // IMPORTANT: this needs to be updated once new events are added
               .AddScoped<IDomainEventHandler<DefaultEnvironmentCreated>, DefaultEnvironmentCreatedHandler>()
               .AddScoped<IDomainEventHandler<EnvironmentCreated>, EnvironmentCreatedHandler>()
               .AddScoped<IDomainEventHandler<EnvironmentDeleted>, EnvironmentDeletedHandler>()
               .AddScoped<IDomainEventHandler<EnvironmentKeysModified>, EnvironmentKeysModifiedHandler>()
               .AddScoped<IDomainEventHandler<EnvironmentKeysImported>, EnvironmentKeysImportedHandler>()
               .AddScoped<IDomainEventHandler<StructureCreated>, StructureCreatedHandler>()
               .AddScoped<IDomainEventHandler<StructureDeleted>, StructureDeletedHandler>()
               .AddScoped<IDomainEventHandler<StructureVariablesModified>, StructureVariablesModifiedHandler>()
               .AddScoped<IDomainEventHandler<ConfigurationBuilt>, ConfigurationBuiltHandler>();

        /// <summary>
        ///     add configuration as a whole, and parts of it
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionConfiguration(this IServiceCollection services, IConfiguration configuration)
            => services.AddSingleton(configuration)
                       .AddSingleton(provider => provider.GetService<IConfiguration>().Get<ProjectionConfiguration>())
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().EventBusConnection)
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().EventStoreConnection)
                       .AddSingleton(provider => provider.GetService<ProjectionConfiguration>().ProjectionStorage);

        /// <summary>
        ///     services for specific tasks within the projection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionServices(this IServiceCollection services)
            => services.AddScoped<IConfigurationParser, AntlrConfigurationParser>()
                       .AddScoped<IConfigurationCompiler, ConfigurationCompiler>()
                       .AddScoped<IConfigurationDatabase, ConfigurationDatabase>()
                       .AddScoped<IJsonTranslator, JsonTranslator>()
                       .AddSingleton(provider =>
                       {
                           var config = provider.GetService<ProjectionConfiguration>()
                                        ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));
                           var eventStoreLogger = provider.GetService<ESLogger>();

                           return EventStoreConnection.Create($"ConnectTo={config.EventStoreConnection.Uri}",
                                                              ConnectionSettings.Create()
                                                                                .PerformOnAnyNode()
                                                                                .PreferRandomNode()
                                                                                .KeepReconnecting()
                                                                                .LimitRetriesForOperationTo(6)
                                                                                .UseCustomLogger(eventStoreLogger),
                                                              config.EventStoreConnection.ConnectionName);
                       })
                       .AddSingleton<IEventDeserializer, EventDeserializer>()
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