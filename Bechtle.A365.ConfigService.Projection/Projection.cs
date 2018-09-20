using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Projection : IProjection
    {
        private ILogger<Projection> Logger { get; }
        private ProjectionConfiguration Configuration { get; }
        private IEventStoreConnection Store { get; set; }
        private IConfigurationDatabase Database { get; set; }

        public Projection(ILoggerFactory loggerFactory,
                          ProjectionConfiguration configuration,
                          IConfigurationDatabase database,
                          IEventStoreConnection store)
        {
            Logger = loggerFactory.CreateLogger<Projection>();

            Logger.LogInformation("starting projection...");

            Configuration = configuration ?? throw new ArgumentNullException(nameof(ProjectionConfiguration));
            Database = database ?? throw new ArgumentNullException(nameof(IConfigurationDatabase));
            Store = store ?? throw new ArgumentNullException(nameof(IEventStoreConnection));
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

            (Database as InMemoryConfigurationDatabase)?.Dump(Logger);
        }

        private async Task Project(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case null:
                    break;

                case EnvironmentCreated environmentCreated:
                    await Database.ModifyEnvironment(environmentCreated.EnvironmentName, environmentCreated.Data);
                    break;

                case EnvironmentUpdated environmentUpdated:
                    await Database.ModifyEnvironment(environmentUpdated.EnvironmentName, environmentUpdated.Data);
                    break;
            
                case SchemaCreated schemaCreated:
                    await Database.ModifySchema(schemaCreated.SchemaName, schemaCreated.Data);
                    break;
                
                case SchemaUpdated schemaUpdated:
                    await Database.ModifySchema(schemaUpdated.SchemaName, schemaUpdated.Data);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainEvent));
            }
        }

        private DomainEvent DeserializeResolvedEvent(ResolvedEvent resolvedEvent)
        {
            var factoryAssociations = new Dictionary<string, Func<byte[], byte[], DomainEvent>>
            {
                {nameof(EnvironmentCreated), EnvironmentCreatedFactory.Deserialize},
                {nameof(EnvironmentUpdated), EnvironmentUpdatedFactory.Deserialize},
                {nameof(SchemaCreated), SchemaCreatedFactory.Deserialize},
                {nameof(SchemaUpdated), SchemaUpdatedFactory.Deserialize}
            };

            foreach (var factory in factoryAssociations)
            {
                if (factory.Key == resolvedEvent.OriginalEvent.EventType)
                    return factory.Value.Invoke(resolvedEvent.OriginalEvent.Data, resolvedEvent.OriginalEvent.Metadata);
            }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            return null;
        }
    }
}