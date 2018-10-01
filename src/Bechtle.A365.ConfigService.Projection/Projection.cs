using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Compilation;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Projection : IProjection
    {
        private ILogger<Projection> Logger { get; }
        private ProjectionConfiguration Configuration { get; }
        private IEventStoreConnection Store { get; }
        private IServiceProvider Provider { get; }
        private IConfigurationCompiler Compiler { get; }
        private IEventDeserializer EventDeserializer { get; }
        private IConfigurationDatabase Database { get; }

        // so many injected things, better not get addicted
        public Projection(ILogger<Projection> logger,
                          ProjectionConfiguration configuration,
                          IConfigurationDatabase database,
                          IEventStoreConnection store,
                          IServiceProvider provider,
                          IConfigurationCompiler compiler,
                          IEventDeserializer eventDeserializer)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Logger.LogInformation("starting projection...");

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            EventDeserializer = eventDeserializer ?? throw new ArgumentNullException(nameof(eventDeserializer));
        }

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            Logger.LogInformation("running projection...");

            await Database.Connect();

            await Store.ConnectAsync();

            Store.SubscribeToStreamFrom(Configuration.EventStore.SubscriptionName,
                                        StreamCheckpoint.StreamStart,
                                        new CatchUpSubscriptionSettings(Configuration.EventStore.MaxLiveQueueSize,
                                                                        Configuration.EventStore.ReadBatchSize,
                                                                        false,
                                                                        true,
                                                                        Configuration.EventStore.SubscriptionName),
                                        EventAppeared,
                                        subscription => { Logger.LogInformation($"subscription to '{subscription.SubscriptionName}' opened"); },
                                        (subscription, reason, exception) =>
                                        {
                                            Logger.LogCritical($"subscription '{subscription.SubscriptionName}' " +
                                                               $"dropped for reason: {reason}; exception {exception}");
                                        });

            while (!cancellationToken.IsCancellationRequested)
                Thread.Sleep(TimeSpan.FromMilliseconds(100));

            Logger.LogInformation("stopping projection...");
        }

        private async Task EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            Logger.LogInformation($"subscription: {subscription.SubscriptionName}; " +
                                  $"EventId: {resolvedEvent.OriginalEvent.EventId}; " +
                                  $"EventType: {resolvedEvent.OriginalEvent.EventType}; " +
                                  $"Created: {resolvedEvent.OriginalEvent.Created}; " +
                                  $"IsJson: {resolvedEvent.OriginalEvent.IsJson}; " +
                                  $"Data: {resolvedEvent.OriginalEvent.Data.Length} bytes; " +
                                  $"Metadata: {resolvedEvent.OriginalEvent.Metadata.Length} bytes;");

            var domainEvent = EventDeserializer.ToDomainEvent(resolvedEvent);

            if (domainEvent == null)
                return;

            await Project(domainEvent);
        }

        private async Task Project(DomainEvent domainEvent)
        {
            // inner function to not clutter this class any more
            Task HandleDomainEvent<T>(T @event) where T : DomainEvent => Provider.GetService<IDomainEventHandler<T>>()
                                                                                 .HandleDomainEvent(@event);

            switch (domainEvent)
            {
                case null:
                    throw new ArgumentNullException(nameof(domainEvent));

                case ConfigurationBuilt configurationBuilt:
                    await HandleDomainEvent(configurationBuilt);
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

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainEvent));
            }
        }
    }
}