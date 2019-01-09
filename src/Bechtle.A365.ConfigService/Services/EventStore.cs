using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.EventFactories;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Utilities;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// god-damn-it fuck you 'EventStore' for creating 'ILogger' when that is essentially a core component of the eco-system
using ESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class EventStore : IEventStore
    {
        private readonly ConfigServiceConfiguration _configuration;
        private readonly IEventStoreConnection _eventStore;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;
        private readonly IEventDeserializer _eventDeserializer;

        /// <summary>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="eventStoreLogger"></param>
        /// <param name="provider"></param>
        /// <param name="eventDeserializer"></param>
        /// <param name="configuration"></param>
        public EventStore(ILogger<EventStore> logger,
                          ESLogger eventStoreLogger,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer,
                          ConfigServiceConfiguration configuration)
        {
            _logger = logger;
            _provider = provider;
            _eventDeserializer = eventDeserializer;
            _configuration = configuration;

            _logger.LogInformation($"connecting to '{configuration.EventStoreConnection.Uri}' " +
                                   $"using connectionName: '{configuration.EventStoreConnection.ConnectionName}'");

            _eventStore = EventStoreConnection.Create(ConnectionSettings.Create()
                                                                        .KeepReconnecting()
                                                                        .KeepRetrying()
                                                                        .UseCustomLogger(eventStoreLogger),
                                                      new Uri(configuration.EventStoreConnection.Uri),
                                                      configuration.EventStoreConnection.ConnectionName);

            _eventStore.ConnectAsync().RunSync();
        }

        /// <inheritdoc />
        public async Task WriteEvent<T>(T domainEvent) where T : DomainEvent
        {
            _logger.LogDebug($"{nameof(WriteEvent)}('{domainEvent.GetType().Name}')");

            var (data, metadata) = SerializeDomainEvent(domainEvent);

            var eventData = new EventData(Guid.NewGuid(),
                                          domainEvent.EventType,
                                          false,
                                          data,
                                          metadata);

            _logger.LogInformation("sending " +
                                   $"EventId: '{eventData.EventId}'; " +
                                   $"Type: '{eventData.Type}'; " +
                                   $"IsJson: '{eventData.IsJson}'; " +
                                   $"Data: {eventData.Data.Length} bytes; " +
                                   $"Metadata: {eventData.Metadata.Length} bytes;");

            var result = await _eventStore.AppendToStreamAsync(_configuration.EventStoreConnection.Stream,
                                                               ExpectedVersion.Any,
                                                               eventData);

            _logger.LogDebug($"sent event '{eventData.EventId}': " +
                             $"NextExpectedVersion: {result.NextExpectedVersion}; " +
                             $"CommitPosition: {result.LogPosition.CommitPosition}; " +
                             $"PreparePosition: {result.LogPosition.PreparePosition};");
        }

        /// <inheritdoc />
        public async Task<IEnumerable<(RecordedEvent, DomainEvent)>> ReplayEvents()
        {
            // readSize must be below 4096
            var readSize = 512;
            long currentPosition = 0;

            var allEvents = new List<(RecordedEvent, DomainEvent)>();
            var continueReading = true;

            do
            {
                var slice = await _eventStore.ReadStreamEventsForwardAsync(_configuration.EventStoreConnection.Stream,
                                                                           currentPosition,
                                                                           readSize,
                                                                           true);

                allEvents.AddRange(slice.Events.Select(e => (e.Event, _eventDeserializer.ToDomainEvent(e))));

                currentPosition = slice.NextEventNumber;
                continueReading = !slice.IsEndOfStream;
            } while (continueReading);

            return allEvents;
        }

        private (byte[] Data, byte[] Metadata) SerializeDomainEvent<T>(T domainEvent) where T : DomainEvent
            => _provider.GetService<IDomainEventSerializer<T>>()
                        .Serialize(domainEvent);
    }
}