using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.EventFactories;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Utilities;
using EventStore.ClientAPI;
using Microsoft.Extensions.Caching.Memory;
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
        private const string CacheKeyReplayedEvents = nameof(CacheKeyReplayedEvents);
        private readonly IMemoryCache _cache;
        private readonly ConfigServiceConfiguration _configuration;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IEventStoreConnection _eventStore;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;

        /// <summary>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="eventStoreLogger"></param>
        /// <param name="provider"></param>
        /// <param name="eventDeserializer"></param>
        /// <param name="cache"></param>
        /// <param name="configuration"></param>
        public EventStore(ILogger<EventStore> logger,
                          ESLogger eventStoreLogger,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer,
                          IMemoryCache cache,
                          ConfigServiceConfiguration configuration)
        {
            _logger = logger;
            _provider = provider;
            _eventDeserializer = eventDeserializer;
            _cache = cache;
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
        public async Task<IEnumerable<(RecordedEvent, DomainEvent)>> ReplayEvents()
        {
            try
            {
                if (_cache.TryGetValue(CacheKeyReplayedEvents, out var cachedEvents))
                {
                    _logger.LogDebug("grabbing replayed events from cache");
                    return (IEnumerable<(RecordedEvent, DomainEvent)>) cachedEvents;
                }
            }
            catch (InvalidCastException)
            {
                // if the cached item is somehow not what we expect,
                // continue as usual and overwrite the cached item
            }

            // readSize must be below 4096
            var readSize = 512;
            long currentPosition = 0;
            var stream = _configuration.EventStoreConnection.Stream;
            var allEvents = new List<(RecordedEvent, DomainEvent)>();
            bool continueReading;

            _logger.LogDebug($"replaying all events from stream '{stream}' using chunks of '{readSize}' per read");

            do
            {
                var slice = await _eventStore.ReadStreamEventsForwardAsync(stream,
                                                                           currentPosition,
                                                                           readSize,
                                                                           true);

                _logger.LogDebug($"read '{slice.Events.Length}' events {slice.FromEventNumber}-{slice.NextEventNumber - 1}/{slice.LastEventNumber}");

                allEvents.AddRange(slice.Events.Select(e => (e.Event, _eventDeserializer.ToDomainEvent(e))));

                currentPosition = slice.NextEventNumber;
                continueReading = !slice.IsEndOfStream;
            } while (continueReading);

            _cache.Set(CacheKeyReplayedEvents, allEvents, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10.0d),
                SlidingExpiration = TimeSpan.FromMinutes(1)
            });

            return allEvents;
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

        private (byte[] Data, byte[] Metadata) SerializeDomainEvent<T>(T domainEvent) where T : DomainEvent
            => _provider.GetService<IDomainEventSerializer<T>>()
                        .Serialize(domainEvent);
    }
}