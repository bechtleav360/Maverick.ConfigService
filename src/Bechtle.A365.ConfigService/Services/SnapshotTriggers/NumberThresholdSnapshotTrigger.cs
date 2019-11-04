using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Services.SnapshotTriggers
{
    /// <summary>
    ///     <see cref="ISnapshotTrigger"/> that triggers once a threshold of DomainEvents,
    ///     that haven't been saved in a snapshot, has been reached
    /// </summary>
    public class NumberThresholdSnapshotTrigger : ISnapshotTrigger
    {
        private readonly IEventStoreConnection _eventStore;
        private readonly IConfiguration _configuration;
        private readonly ProjectionConfiguration _projectionConfig;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public NumberThresholdSnapshotTrigger(IEventStoreConnection eventStore,
                                              IConfiguration configuration,
                                              ProjectionConfiguration projectionConfig,
                                              ILogger<NumberThresholdSnapshotTrigger> logger)
        {
            _eventStore = eventStore;
            _configuration = configuration;
            _projectionConfig = projectionConfig;
            _logger = logger;
        }

        /// <inheritdoc />
        public event EventHandler SnapshotTriggered;

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug("connecting to EventStore");
                await _eventStore.ConnectAsync();

                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug($"retrieving last event in '{_projectionConfig.EventStoreConnection.Stream}'");
                var result = await _eventStore.ReadStreamEventsBackwardAsync(_projectionConfig.EventStoreConnection.Stream, StreamPosition.End, 1, true);
                var lastEventNumber = result.LastEventNumber;
                _logger.LogDebug($"last event in '{_projectionConfig.EventStoreConnection.Stream}': {lastEventNumber}");

                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug("retrieving event-number of last Snapshot");
                var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
                _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

                var threshold = _configuration.GetSection("SnapshotConfiguration:Triggers:NumberThreshold:Threshold").Get<long>();
                _logger.LogDebug($"resolved threshold: {threshold}");

                if (lastEventNumber - currentSnapshotEventNumber > threshold)
                {
                    TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                await _eventStore.SubscribeToStreamAsync(
                    _projectionConfig.EventStoreConnection.Stream,
                    true,
                    EventAppeared,
                    SubscriptionDropped);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error in {GetType().Name}, trigger will not fire anymore");
            }
        }

        private void SubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception e)
            => _logger.LogInformation(e, $"subscription to {subscription.StreamId} has been dropped: {reason:G} {e.Message}");

        private async Task EventAppeared(EventStoreSubscription subscription, ResolvedEvent reason)
        {
            var lastEventNumber = subscription.LastEventNumber ?? 0;
            _logger.LogDebug($"event received, EventNumber: {lastEventNumber}");

            _logger.LogDebug("retrieving event-number of last Snapshot");
            var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
            _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

            var threshold = _configuration.GetSection("SnapshotConfiguration:Triggers:NumberThreshold:Threshold").Get<long>();
            _logger.LogDebug($"resolved threshold: {threshold}");

            if (lastEventNumber - currentSnapshotEventNumber > threshold)
                TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
        }

        private void TriggerSnapshot(long lastEventNumber, long currentSnapshotEventNumber, long threshold)
        {
            _logger.LogInformation($"triggering '{nameof(SnapshotTriggered)}' event because threshold has already been crossed by " +
                                   $"'{lastEventNumber - currentSnapshotEventNumber - threshold}'" +
                                   $"(lastEvent: {lastEventNumber}; currentSnapshot: {currentSnapshotEventNumber}; threshold: {threshold})");

            try
            {
                _eventStore.Close();
                _eventStore.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "error while disposing of EventStore while triggering new Snapshot");
            }

            SnapshotTriggered?.Invoke(this, EventArgs.Empty);
        }

        private Task<long> GetCurrentSnapshotEventNumber() => Task.FromResult(0L);

        /// <inheritdoc />
        public void Dispose()
        {
            _eventStore?.Close();
            _eventStore?.Dispose();
        }
    }
}