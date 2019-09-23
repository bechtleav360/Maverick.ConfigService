using System;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.ConfigService.Projection.Services;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Abstraction;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Projection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     add logging for ourselves, and EventStore
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddCustomLogging(this IServiceCollection services, ILogger logger)
            => services.AddSingleton(logger, typeof(ILogger<>), typeof(Logger<>))
                       .AddSingleton<ESLogger, EventStoreLogger>(logger);

        /// <summary>
        ///     add services to handle DomainEvent tasks
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddDomainEventServices(this IServiceCollection services, ILogger logger)
            => services
               // add DomainEventConverter as generic class for IDomainEventConverter
               .AddSingleton(logger, typeof(IDomainEventConverter<>), typeof(DomainEventConverter<>))
               // register all IDomainEventHandlers
               // IMPORTANT: this needs to be updated once new events are added
               .AddScoped<IDomainEventHandler<DefaultEnvironmentCreated>, DefaultEnvironmentCreatedHandler>(logger)
               .AddScoped<IDomainEventHandler<EnvironmentCreated>, EnvironmentCreatedHandler>(logger)
               .AddScoped<IDomainEventHandler<EnvironmentDeleted>, EnvironmentDeletedHandler>(logger)
               .AddScoped<IDomainEventHandler<EnvironmentKeysModified>, EnvironmentKeysModifiedHandler>(logger)
               .AddScoped<IDomainEventHandler<EnvironmentKeysImported>, EnvironmentKeysImportedHandler>(logger)
               .AddScoped<IDomainEventHandler<StructureCreated>, StructureCreatedHandler>(logger)
               .AddScoped<IDomainEventHandler<StructureDeleted>, StructureDeletedHandler>(logger)
               .AddScoped<IDomainEventHandler<StructureVariablesModified>, StructureVariablesModifiedHandler>(logger)
               .AddScoped<IDomainEventHandler<ConfigurationBuilt>, ConfigurationBuiltHandler>(logger);

        /// <summary>
        ///     add configuration as a whole, and parts of it
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionConfiguration(this IServiceCollection services, ILogger logger, IConfiguration configuration)
            => services.AddSingleton(logger, configuration)
                       .AddSingleton(logger, provider => provider.GetService<IConfiguration>().Get<ProjectionConfiguration>())
                       .AddSingleton(logger, provider => provider.GetService<ProjectionConfiguration>().EventBusConnection)
                       .AddSingleton(logger, provider => provider.GetService<ProjectionConfiguration>().EventStoreConnection)
                       .AddSingleton(logger, provider => provider.GetService<ProjectionConfiguration>().ProjectionStorage);

        /// <summary>
        ///     services for specific tasks within the projection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddProjectionServices(this IServiceCollection services, ILogger logger)
            => services.AddScoped<IConfigurationParser, AntlrConfigurationParser>(logger)
                       .AddScoped<IConfigurationCompiler, ConfigurationCompiler>(logger)
                       .AddScoped<IConfigurationDatabase, ConfigurationDatabase>(logger)
                       .AddScoped<IJsonTranslator, JsonTranslator>(logger)
                       .AddTransient(logger, provider =>
                       {
                           var config = provider.GetService<ProjectionConfiguration>()
                                        ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));
                           var eventStoreLogger = provider.GetService<ESLogger>();

                           return EventStoreConnection.Create($"ConnectTo={config.EventStoreConnection.Uri}",
                                                              ConnectionSettings.Create()
                                                                                .PerformOnAnyNode()
                                                                                .PreferRandomNode()
                                                                                .KeepReconnecting()
                                                                                .SetReconnectionDelayTo(TimeSpan.FromSeconds(60))
                                                                                .LimitRetriesForOperationTo(6)
                                                                                .LimitConcurrentOperationsTo(1)
                                                                                .EnableVerboseLogging()
                                                                                .UseCustomLogger(eventStoreLogger),
                                                              config.EventStoreConnection.ConnectionName);
                       })
                       .AddSingleton<IEventDeserializer, EventDeserializer>(logger)
                       .AddSingleton<IEventBusService, EventBusService>(logger)
                       .AddTransient<IEventBus, WebSocketEventBusClient>(logger, provider =>
                       {
                           var config = provider.GetService<EventBusConnectionConfiguration>();

                           return new WebSocketEventBusClient(new Uri(new Uri(config.Server), config.Hub).ToString(),
                                                              provider.GetService<ILoggerFactory>());
                       });
    }
}