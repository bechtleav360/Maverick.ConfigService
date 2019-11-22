using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Implementations.SnapshotTriggers
{
    /// <summary>
    ///     <see cref="ISnapshotTrigger" /> that triggers once a threshold of DomainEvents,
    ///     that haven't been saved in a snapshot, has been reached
    /// </summary>
    public sealed class NumberThresholdSnapshotTrigger : ISnapshotTrigger
    {
        private readonly IEventStore _eventStore;
        private readonly ILogger _logger;
        private readonly ISnapshotStore _snapshotStore;

        private IConfiguration _configuration;

        /// <inheritdoc />
        public NumberThresholdSnapshotTrigger(IEventStore eventStore,
                                              ISnapshotStore snapshotStore,
                                              ILogger<NumberThresholdSnapshotTrigger> logger)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _eventStore.EventAppeared -= EventStoreOnEventAppeared;
        }

        /// <inheritdoc />
        public void Configure(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs> SnapshotTriggered;

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var threshold = _configuration.GetSection("Max").Get<long>();
                _logger.LogDebug($"resolved threshold: {threshold}");

                if (threshold > 0)
                {
                    _logger.LogDebug("retrieving last event in EventStore");
                    var lastEventNumber = await _eventStore.GetCurrentEventNumber();
                    _logger.LogDebug($"last event in EventStore: {lastEventNumber}");

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    _logger.LogDebug("retrieving event-number of last Snapshot");
                    var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
                    _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

                    if (lastEventNumber - currentSnapshotEventNumber > threshold)
                    {
                        TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;
                }
                else
                {
                    _logger.LogInformation($"invalid threshold set: {threshold}; " +
                                           "select a positive threshold greater than 0; " +
                                           "skipping immediate threshold-check");
                }

                _eventStore.EventAppeared += EventStoreOnEventAppeared;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error in {GetType().Name}, trigger will not fire anymore");
            }
        }

        private async void EventStoreOnEventAppeared(object sender, (EventStoreSubscription Subscription, ResolvedEvent ResolvedEvent) e)
        {
            var (subscription, _) = e;

            var threshold = _configuration.GetSection("Max").Get<long>();
            _logger.LogDebug($"resolved threshold: {threshold}");

            if (threshold <= 0)
            {
                _logger.LogInformation($"invalid threshold set: {threshold}; select a positive threshold greater than 0");
                return;
            }

            var lastEventNumber = subscription.LastEventNumber ?? 0;
            _logger.LogDebug($"event received, EventNumber: {lastEventNumber}");

            _logger.LogDebug("retrieving event-number of last Snapshot");
            var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
            _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

            if (lastEventNumber - currentSnapshotEventNumber > threshold)
                TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
        }

        private async Task<long> GetCurrentSnapshotEventNumber()
        {
            var result = await _snapshotStore.GetLatestSnapshotNumbers();
            return result?.Data ?? 0;
        }

        private void TriggerSnapshot(long lastEventNumber, long currentSnapshotEventNumber, long threshold)
        {
            _logger.LogInformation($"triggering '{nameof(SnapshotTriggered)}' event because threshold has already been crossed by " +
                                   $"'{lastEventNumber - currentSnapshotEventNumber - threshold}'" +
                                   $"(lastEvent: {lastEventNumber}; currentSnapshot: {currentSnapshotEventNumber}; threshold: {threshold})");

            SnapshotTriggered?.Invoke(this, EventArgs.Empty);
        }
    }
}