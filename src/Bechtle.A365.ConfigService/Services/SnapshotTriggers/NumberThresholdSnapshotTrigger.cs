using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Services.Stores;
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
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ConfigServiceConfiguration _serviceConfig;
        private readonly ILogger _logger;

        private IConfiguration _configuration;

        /// <inheritdoc />
        public NumberThresholdSnapshotTrigger(IEventStore eventStore,
                                              ISnapshotStore snapshotStore,
                                              ConfigServiceConfiguration serviceConfig,
                                              ILogger<NumberThresholdSnapshotTrigger> logger)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _serviceConfig = serviceConfig;
            _logger = logger;
        }

        /// <inheritdoc />
        public event EventHandler SnapshotTriggered;

        /// <inheritdoc />
        public void Configure(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug($"retrieving last event in '{_serviceConfig.EventStoreConnection.Stream}'");
                var lastEventNumber = await _eventStore.GetCurrentEventNumber();
                _logger.LogDebug($"last event in '{_serviceConfig.EventStoreConnection.Stream}': {lastEventNumber}");

                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug("retrieving event-number of last Snapshot");
                var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
                _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

                var threshold = _configuration.GetSection("Max").Get<long>();
                _logger.LogDebug($"resolved threshold: {threshold}");

                if (lastEventNumber - currentSnapshotEventNumber > threshold)
                {
                    TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                _eventStore.EventAppeared += EventStoreOnEventAppeared;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error in {GetType().Name}, trigger will not fire anymore");
            }
        }

        // @TODO: see if this can be done without 'async void'
        private async void EventStoreOnEventAppeared(object sender, (EventStoreSubscription Subscription, ResolvedEvent ResolvedEvent) e)
        {
            var (subscription, _) = e;

            var lastEventNumber = subscription.LastEventNumber ?? 0;
            _logger.LogDebug($"event received, EventNumber: {lastEventNumber}");

            _logger.LogDebug("retrieving event-number of last Snapshot");
            var currentSnapshotEventNumber = await GetCurrentSnapshotEventNumber();
            _logger.LogDebug($"event-number of last Snapshot: {currentSnapshotEventNumber}");

            var threshold = _configuration.GetSection("Max").Get<long>();
            _logger.LogDebug($"resolved threshold: {threshold}");

            if (lastEventNumber - currentSnapshotEventNumber > threshold)
                TriggerSnapshot(lastEventNumber, currentSnapshotEventNumber, threshold);
        }

        private void TriggerSnapshot(long lastEventNumber, long currentSnapshotEventNumber, long threshold)
        {
            _logger.LogInformation($"triggering '{nameof(SnapshotTriggered)}' event because threshold has already been crossed by " +
                                   $"'{lastEventNumber - currentSnapshotEventNumber - threshold}'" +
                                   $"(lastEvent: {lastEventNumber}; currentSnapshot: {currentSnapshotEventNumber}; threshold: {threshold})");

            SnapshotTriggered?.Invoke(this, EventArgs.Empty);
        }

        private async Task<long> GetCurrentSnapshotEventNumber()
        {
            var result = await _snapshotStore.GetLatestSnapshotNumbers();
            return result?.Data ?? 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _eventStore.EventAppeared -= EventStoreOnEventAppeared;
        }
    }
}