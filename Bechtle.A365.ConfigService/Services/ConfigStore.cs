using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainEvents;
using Bechtle.A365.ConfigService.Utilities;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// god-damn-it fuck you 'EventStore' for creating 'ILogger' when that is basically a core component of the eco-system
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Services
{
    public class ConfigStore : IConfigStore
    {
        private readonly IEventStoreConnection _eventStore;

        private readonly ILogger _logger;

        private readonly string _stream;

        private List<DomainEvent> _events;

        public ConfigStore(IServiceProvider provider)
        {
            _stream = "ConfigStream";

            _events = new List<DomainEvent>();

            _logger = provider.GetService<ILoggerFactory>()
                              .CreateLogger<ConfigStore>();

            var uri = new Uri("tcp://admin:changeit@localhost:1113");
            var connectionName = "ConfigService";

            _logger.LogInformation($"connecting to '{uri}' using connectionName: '{connectionName}'");

            _eventStore = EventStoreConnection.Create(uri, connectionName);

            _eventStore.ConnectAsync().RunSync();

            SubscribeToConfigEvents();
        }

        private void SubscribeToConfigEvents()
        {
            _eventStore.SubscribeToStreamFrom(_stream,
                                              StreamCheckpoint.StreamStart,
                                              new CatchUpSubscriptionSettings(100, 100, false, true, "ConfigServiceReader"),
                                              EventAppeared,
                                              LiveProcessingStarted,
                                              SubscriptionDropped);
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception)
        {
            _logger.LogWarning($"subscription to '{subscription.SubscriptionName}' dropped; " +
                               $"reason: {reason}; " +
                               $"exception: {exception};");
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription subscription)
        {
            _logger.LogInformation($"subscription to '{subscription.SubscriptionName}' started");
        }

        private Task EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent domainEvent)
        {
            _logger.LogInformation($"event appeared in '{subscription.SubscriptionName}': " +
                                   $"{domainEvent.OriginalEvent.Created:O} {domainEvent.OriginalEvent.EventType}");

            _events.Add(DomainEvent.From(domainEvent.OriginalEvent));

            return Task.CompletedTask;
        }

        public async Task WriteEvent(DomainEvent domainEvent)
        {
            _logger.LogDebug($"{nameof(WriteEvent)}('{domainEvent.GetType().Name}')");

            var eventData = new EventData(Guid.NewGuid(),
                                          domainEvent.EventType,
                                          false,
                                          domainEvent.Serialize(),
                                          domainEvent.SerializeMetadata());

            _logger.LogInformation("sending " +
                                   $"EventId: '{eventData.EventId}'; " +
                                   $"Type: '{eventData.Type}'; " +
                                   $"IsJson: '{eventData.IsJson}'; " +
                                   $"Data: {eventData.Data.Length} bytes; " +
                                   $"Metadata: {eventData.Metadata.Length} bytes;");

            var result = await _eventStore.AppendToStreamAsync(_stream, ExpectedVersion.Any, eventData);

            _logger.LogDebug($"sent event '{eventData.EventId}': " +
                             $"NextExpectedVersion: {result.NextExpectedVersion}; " +
                             $"CommitPosition: {result.LogPosition.CommitPosition}; " +
                             $"PreparePosition: {result.LogPosition.PreparePosition};");
        }

        public DomainEvent[] GetAll() => _events.ToArray();
    }
}