﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bechtle.A365.ConfigService.Projection
{
    // Class is Instantiated via DependencyInjection
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Projection : HostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly ILogger<Projection> _logger;
        private readonly IServiceProvider _provider;
        private readonly IEventStoreConnection _store;

        // so many injected things, better not get addicted
        public Projection(ILogger<Projection> logger,
                          IConfiguration configuration,
                          IEventStoreConnection store,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer)
            : base(provider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("starting projection...");

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _eventDeserializer = eventDeserializer ?? throw new ArgumentNullException(nameof(eventDeserializer));

            _logger.LogInformation("registering config-reload hook");

            ChangeToken.OnChange(configuration.GetReloadToken,
                                 conf =>
                                 {
                                     var projectionConfiguration = conf?.Get<ProjectionConfiguration>();
                                     Program.ConfigureNLog(projectionConfiguration?.LoggingConfiguration);
                                     _logger.LogInformation(FormatConfiguration(projectionConfiguration));
                                 },
                                 _configuration);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //return Context to ApplicationLiveTime.OnStart to prevent Service Start exception (timeout on Service start)
            await Task.Yield();

            var config = _configuration.Get<ProjectionConfiguration>();

            _logger.LogInformation(FormatConfiguration(config));

            _logger.LogInformation("running projection...");

            long? latestEvent;
            using (var scope = _provider.CreateScope())
            {
                var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();
                await database.Connect();
                latestEvent = await database.GetLatestProjectedEventId();
            }

            await _store.ConnectAsync();

            _store.SubscribeToStreamFrom(config.EventStoreConnection.SubscriptionName,
                                         latestEvent,
                                         new CatchUpSubscriptionSettings(config.EventStoreConnection.MaxLiveQueueSize,
                                                                         config.EventStoreConnection.ReadBatchSize,
                                                                         false,
                                                                         true,
                                                                         config.EventStoreConnection.SubscriptionName),
                                         EventAppeared,
                                         subscription => { _logger.LogInformation($"subscription to '{subscription.SubscriptionName}' opened"); },
                                         (subscription, reason, exception) =>
                                         {
                                             _logger.LogCritical($"subscription '{subscription.SubscriptionName}' " +
                                                                 $"dropped for reason: {reason}; exception {exception}");
                                         });

            while (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

            _logger.LogInformation("stopping projection...");
        }

        private string FormatConfiguration(ProjectionConfiguration config)
        {
            var settings = new JsonSerializerSettings {Formatting = Formatting.Indented};
            settings.Converters.Add(new StringEnumConverter());

            return $"using configuration (may change during runtime): \r\n" +
                   $"{JsonConvert.SerializeObject(config, settings)}";
        }

        private async Task EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            _logger.LogInformation($"Stream: {resolvedEvent.OriginalStreamId}#{resolvedEvent.OriginalEventNumber}; " +
                                   $"EventId: {resolvedEvent.OriginalEvent.EventId}; " +
                                   $"EventType: {resolvedEvent.OriginalEvent.EventType}; " +
                                   $"Created: {resolvedEvent.OriginalEvent.Created}; " +
                                   $"IsJson: {resolvedEvent.OriginalEvent.IsJson}; " +
                                   $"Data: {resolvedEvent.OriginalEvent.Data.Length} bytes; " +
                                   $"Metadata: {resolvedEvent.OriginalEvent.Metadata.Length} bytes;");

            var domainEvent = _eventDeserializer.ToDomainEvent(resolvedEvent);

            if (domainEvent == null)
                return;

            using (var scope = _provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ProjectionStoreContext>();
                var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();

                using (var transaction = context.Database.BeginTransaction())
                {
                    _logger.LogInformation($"using transaction '{transaction.TransactionId}' for event '{domainEvent.EventType}'");

                    try
                    {
                        _logger.LogDebug($"projecting event '{domainEvent.EventType}'");

                        await Project(domainEvent, scope.ServiceProvider);

                        _logger.LogDebug($"recording successful projection of event #{resolvedEvent.OriginalEventNumber} to database");

                        await database.SetLatestProjectedEventId(resolvedEvent.OriginalEventNumber);

                        _logger.LogInformation("saving changes made to the database...");

                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug($"saving '{context.ChangeTracker.Entries().Count()}' changes made to the database...");

                        await context.SaveChangesAsync();

                        _logger.LogInformation($"committing transaction '{transaction.TransactionId}'");

                        transaction.Commit();

                        _logger.LogInformation($"transaction '{transaction.TransactionId}' committed");
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogCritical($"could not project domain-event of type '{domainEvent.EventType}', " +
                                            $"rolling back transaction '{transaction.TransactionId}' :{e}");
                    }
                    finally
                    {
                        _logger.LogTrace("forcing GC.Collect...");

                        GC.Collect();
                    }
                }
            }
        }

        private async Task Project(DomainEvent domainEvent, IServiceProvider provider)
        {
            // inner function to not clutter this class any more
            Task HandleDomainEvent<T>(T @event) where T : DomainEvent => provider.GetService<IDomainEventHandler<T>>()
                                                                                 .HandleDomainEvent(@event);

            switch (domainEvent)
            {
                case null:
                    throw new ArgumentNullException(nameof(domainEvent));

                case ConfigurationBuilt configurationBuilt:
                    await HandleDomainEvent(configurationBuilt);
                    break;

                case DefaultEnvironmentCreated defaultEnvironmentCreated:
                    await HandleDomainEvent(defaultEnvironmentCreated);
                    break;

                case EnvironmentCreated environmentCreated:
                    await HandleDomainEvent(environmentCreated);
                    break;

                case EnvironmentDeleted environmentDeleted:
                    await HandleDomainEvent(environmentDeleted);
                    break;

                case EnvironmentKeysModified environmentKeyModified:
                    await HandleDomainEvent(environmentKeyModified);
                    break;

                case EnvironmentKeysImported environmentKeysImported:
                    await HandleDomainEvent(environmentKeysImported);
                    break;

                case StructureCreated structureCreated:
                    await HandleDomainEvent(structureCreated);
                    break;

                case StructureDeleted structureDeleted:
                    await HandleDomainEvent(structureDeleted);
                    break;

                case StructureVariablesModified structureVariablesModified:
                    await HandleDomainEvent(structureVariablesModified);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainEvent));
            }
        }
    }
}