﻿using System;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
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
                    .AddSingleton<IConfigurationDatabase, InMemoryConfigurationDatabase>()
                    .AddSingleton<IDomainEventSerializer<EnvironmentCreated>, EnvironmentCreatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<EnvironmentUpdated>, EnvironmentUpdatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<VersionCompiled>, VersionCompiledSerializer>()
                    .AddSingleton<IDomainEventSerializer<SchemaCreated>, SchemaCreatedSerializer>()
                    .AddSingleton<IDomainEventSerializer<SchemaUpdated>, SchemaUpdatedSerializer>();
        }
    }
}