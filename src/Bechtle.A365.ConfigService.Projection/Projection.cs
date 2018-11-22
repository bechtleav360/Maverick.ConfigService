using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Projection : HostedService
    {
        private readonly ILogger<Projection> _logger;
        private readonly ProjectionConfiguration _configuration;
        private readonly IEventStoreConnection _store;
        private readonly IServiceProvider _provider;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IConfigurationDatabase _database;

        // so many injected things, better not get addicted
        public Projection(ILogger<Projection> logger,
                          ProjectionConfiguration configuration,
                          IConfigurationDatabase database,
                          IEventStoreConnection store,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("starting projection...");

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _eventDeserializer = eventDeserializer ?? throw new ArgumentNullException(nameof(eventDeserializer));
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
            try
            {
                await Project(domainEvent);

                await _database.SetLatestProjectedEventId(resolvedEvent.OriginalEventNumber);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"could not project domain-event of type '{domainEvent.EventType}'");
            }
        }

        private async Task Project(DomainEvent domainEvent)
        {
            // inner function to not clutter this class any more
            Task HandleDomainEvent<T>(T @event) where T : DomainEvent => _provider.GetService<IDomainEventHandler<T>>()
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

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("running projection...");

            await _database.Connect();

            await _store.ConnectAsync();

            var latestEvent = await _database.GetLatestProjectedEventId();

            _store.SubscribeToStreamFrom(_configuration.EventStore.SubscriptionName,
                                         latestEvent,
                                         new CatchUpSubscriptionSettings(_configuration.EventStore.MaxLiveQueueSize,
                                                                         _configuration.EventStore.ReadBatchSize,
                                                                         false,
                                                                         true,
                                                                         _configuration.EventStore.SubscriptionName),
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
    }
}