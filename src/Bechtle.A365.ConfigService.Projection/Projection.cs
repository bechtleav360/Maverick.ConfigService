using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
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
        private IConfigurationDatabase Database { get; }

        public Projection(ILoggerFactory loggerFactory,
                          ProjectionConfiguration configuration,
                          IConfigurationDatabase database,
                          IEventStoreConnection store,
                          IServiceProvider provider,
                          IConfigurationCompiler compiler)
        {
            Logger = loggerFactory.CreateLogger<Projection>();

            Logger.LogInformation("starting projection...");

            Configuration = configuration ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));
            Database = database ?? throw new ArgumentNullException(nameof(IConfigurationDatabase));
            Store = store ?? throw new ArgumentNullException(nameof(IEventStoreConnection));
            Provider = provider;
            Compiler = compiler;
        }

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            Logger.LogInformation("running projection...");

            await Database.Connect();

            await Store.ConnectAsync();

            Store.SubscribeToStreamFrom(Configuration.SubscriptionName,
                                        StreamCheckpoint.StreamStart,
                                        new CatchUpSubscriptionSettings(Configuration.MaxLiveQueueSize,
                                                                        Configuration.ReadBatchSize,
                                                                        false,
                                                                        true,
                                                                        Configuration.SubscriptionName),
                                        EventAppeared,
                                        LiveProcessingStarted,
                                        SubscriptionDropped);

            while (!cancellationToken.IsCancellationRequested)
                Thread.Sleep(TimeSpan.FromMilliseconds(100));

            Logger.LogInformation("stopping projection...");
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription subscription,
                                         SubscriptionDropReason reason,
                                         Exception exception)
        {
            Logger.LogCritical($"subscription '{subscription.SubscriptionName}' dropped for reason: {reason}; exception {exception}");
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription subscription)
        {
            Logger.LogInformation($"subscription to '{subscription.SubscriptionName}' opened");
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

            var domainEvent = DeserializeResolvedEvent(resolvedEvent);

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

                case EnvironmentKeyModified environmentKeyModified:
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

        private DomainEvent DeserializeResolvedEvent(ResolvedEvent resolvedEvent)
        {
            var factoryAssociations = new Dictionary<string, Type>
            {
                {DomainEvent.GetEventType<ConfigurationBuilt>(), typeof(IDomainEventSerializer<ConfigurationBuilt>)},
                {DomainEvent.GetEventType<EnvironmentCreated>(), typeof(IDomainEventSerializer<EnvironmentCreated>)},
                {DomainEvent.GetEventType<EnvironmentDeleted>(), typeof(IDomainEventSerializer<EnvironmentDeleted>)},
                {DomainEvent.GetEventType<EnvironmentKeyModified>(), typeof(IDomainEventSerializer<EnvironmentKeyModified>)},
                {DomainEvent.GetEventType<StructureCreated>(), typeof(IDomainEventSerializer<StructureCreated>)},
                {DomainEvent.GetEventType<StructureDeleted>(), typeof(IDomainEventSerializer<StructureDeleted>)}
            };

            foreach (var factory in factoryAssociations)
            {
                if (factory.Key != resolvedEvent.OriginalEvent.EventType)
                    continue;

                var serializer = (IDomainEventSerializer) Provider.GetService(factory.Value);

                return serializer.Deserialize(resolvedEvent.OriginalEvent.Data, resolvedEvent.OriginalEvent.Metadata);
            }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            return null;
        }
    }
}