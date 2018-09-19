using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection
{
    public class Projection : IProjection
    {
        public ILogger<Projection> Logger { get; }
        public ProjectionConfiguration Configuration { get; }

        private IEventStoreConnection _eventStore;

        public Projection(ILoggerFactory loggerFactory, ProjectionConfiguration configuration)
        {
            Logger = loggerFactory.CreateLogger<Projection>();
            Configuration = configuration;
        }

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            Logger.LogInformation("starting projection...");

            Logger.LogInformation("running projection...");

            _eventStore = EventStoreConnection.Create(new Uri(Configuration.EventStoreUri), Configuration.ConnectionName);

            await _eventStore.ConnectAsync();

            _eventStore.SubscribeToAllFrom(Position.Start,
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

        private Task EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            Logger.LogInformation($"subscription: {subscription.SubscriptionName}; " +
                                  $"EventId: {resolvedEvent.OriginalEvent.EventId}; " +
                                  $"EventType: {resolvedEvent.OriginalEvent.EventType}; " +
                                  $"Created: {resolvedEvent.OriginalEvent.Created}; " +
                                  $"IsJson: {resolvedEvent.OriginalEvent.IsJson}; " +
                                  $"Data: {resolvedEvent.OriginalEvent.Data.Length} bytes; " +
                                  $"Metadata: {resolvedEvent.OriginalEvent.Metadata.Length} bytes; ");

            return Task.CompletedTask;
        }
    }
}