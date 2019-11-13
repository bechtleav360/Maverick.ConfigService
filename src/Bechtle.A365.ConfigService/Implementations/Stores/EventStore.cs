using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
// god-damn-it fuck you 'EventStore' for creating 'ILogger' when that is essentially a core component of the eco-system
using ESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IEventStore" />
    public class EventStore : IEventStore, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly object _connectionLock;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly ESLogger _eventStoreLogger;
        private readonly ILogger _logger;
        private readonly IMetrics _metrics;
        private readonly IServiceProvider _provider;

        private IEventStoreConnection _eventStore;
        private EventStoreConnectionConfiguration _eventStoreConfiguration;
        private EventStoreSubscription _eventSubscription;

        /// <inheritdoc />
        /// <param name="logger"></param>
        /// <param name="eventStoreLogger"></param>
        /// <param name="provider"></param>
        /// <param name="eventDeserializer"></param>
        /// <param name="configuration"></param>
        /// <param name="metrics"></param>
        public EventStore(ILogger<EventStore> logger,
                          ESLogger eventStoreLogger,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer,
                          IConfiguration configuration,
                          IMetrics metrics)
        {
            _connectionLock = new object();
            _logger = logger;
            _eventStoreLogger = eventStoreLogger;
            _provider = provider;
            _eventDeserializer = eventDeserializer;
            _configuration = configuration;
            _metrics = metrics;

            ChangeToken.OnChange(_configuration.GetReloadToken, OnConfigurationChanged);
        }

        /// <inheritdoc cref="Interfaces.ConnectionState" />
        public static ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

        /// <inheritdoc />
        public void Dispose()
        {
            _eventSubscription?.Dispose();
            _eventStore?.Dispose();
        }

        /// <inheritdoc />
        public event EventHandler<(EventStoreSubscription Subscription, ResolvedEvent ResolvedEvent)> EventAppeared;

        /// <inheritdoc />
        public async Task<long> GetCurrentEventNumber()
        {
            // connect if we're not already connected
            Connect();

            var result = await _eventStore.ReadStreamEventsBackwardAsync(_eventStoreConfiguration.Stream, StreamPosition.End, 1, true);

            return result.LastEventNumber;
        }

        /// <inheritdoc />
        public Task ReplayEventsAsStream(Func<(RecordedEvent RecordedEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                         int readSize = 64,
                                         StreamDirection direction = StreamDirection.Forwards,
                                         long startIndex = -1)
            => ReplayEventsAsStream(_ => true, streamProcessor, readSize, direction, startIndex);

        /// <inheritdoc />
        public async Task ReplayEventsAsStream(Func<(RecordedEvent RecordedEvent, DomainEventMetadata Metadata), bool> streamFilter,
                                               Func<(RecordedEvent RecordedEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                               int readSize = 64,
                                               StreamDirection direction = StreamDirection.Forwards,
                                               long startIndex = -1)
        {
            // connect if we're not already connected
            Connect();

            if (streamProcessor is null)
                return;

            // readSize must be below 4096
            readSize = Math.Min(readSize, 4096);

            // use either the given start-position (if startIndex positive or 0), or the Beginning (depending on Direction)
            var currentPosition = startIndex >= 0
                                      ? startIndex
                                      : direction == StreamDirection.Forwards
                                          ? StreamPosition.Start
                                          : StreamPosition.End;

            var stream = _eventStoreConfiguration.Stream;
            bool continueReading;

            _logger.LogDebug($"replaying all events from stream '{stream}' using chunks of '{readSize}' per read, " +
                             (direction == StreamDirection.Forwards ? "forwards from the start" : "backwards from the end"));

            do
            {
                var slice = await (direction == StreamDirection.Forwards
                                       ? _eventStore.ReadStreamEventsForwardAsync(stream, currentPosition, readSize, true)
                                       : _eventStore.ReadStreamEventsBackwardAsync(stream, currentPosition, readSize, true));

                _logger.LogDebug($"read '{slice.Events.Length}' events " +
                                 $"{slice.FromEventNumber}-{slice.NextEventNumber - 1}/{slice.LastEventNumber} {direction}");

                _metrics.Measure.Counter.Increment(KnownMetrics.EventsRead, slice.Events.Length);

                foreach (var item in slice.Events)
                {
                    // if we can't deserialize the metadata and execute the filter on it we skip this event entirely
                    if (_eventDeserializer.ToMetadata(item, out var metadata)
                        && !streamFilter((RecordedEvent: item.Event, Metadata: metadata)))
                    {
                        _metrics.Measure.Counter.Increment(KnownMetrics.EventsFiltered, item.Event.EventType);
                        continue;
                    }

                    if (!_eventDeserializer.ToDomainEvent(item, out var domainEvent))
                    {
                        _logger.LogWarning($"event {item.Event.EventId}#{item.Event.EventNumber} could not be deserialized");
                        continue;
                    }

                    _metrics.Measure.Counter.Increment(KnownMetrics.EventsStreamed, domainEvent.EventType);

                    // stop streaming events once streamProcessor returns false
                    if (!streamProcessor.Invoke((item.Event, domainEvent)))
                        return;
                }

                currentPosition = slice.NextEventNumber;
                continueReading = !slice.IsEndOfStream;
            } while (continueReading);
        }

        /// <inheritdoc />
        public async Task<long> WriteEvents(IList<DomainEvent> domainEvents)
        {
            // connect if we're not already connected
            Connect();

            var eventList = string.Join(", ", domainEvents.Select(e => e.EventType));

            _logger.LogDebug($"{nameof(WriteEvents)}('{eventList}')");

            var eventData = domainEvents.Select(e =>
            {
                var (data, metadata) = SerializeDomainEvent(e);

                return new EventData(Guid.NewGuid(),
                                     e.EventType,
                                     true,
                                     data,
                                     metadata);
            }).ToList();

            foreach (var item in eventData)
                _logger.LogInformation("sending " +
                                       $"EventId: '{item.EventId}'; " +
                                       $"Type: '{item.Type}'; " +
                                       $"IsJson: '{item.IsJson}'; " +
                                       $"Data: {item.Data.Length} bytes; " +
                                       $"Metadata: {item.Metadata.Length} bytes;");

            var result = await _eventStore.AppendToStreamAsync(_eventStoreConfiguration.Stream,
                                                               ExpectedVersion.Any,
                                                               eventData);

            foreach (var item in domainEvents)
                _metrics.Measure.Counter.Increment(KnownMetrics.EventsWritten, item.EventType);

            _logger.LogDebug($"sent {domainEvents.Count} events '{eventList}': " +
                             $"NextExpectedVersion: {result.NextExpectedVersion}; " +
                             $"CommitPosition: {result.LogPosition.CommitPosition}; " +
                             $"PreparePosition: {result.LogPosition.PreparePosition};");

            return result.NextExpectedVersion;
        }

        private void Connect(bool reconnect = false)
        {
            // use lock to prevent multiple simultaneous calls to Connect causing multiple connections
            lock (_connectionLock)
            {
                // only continue if either reconnect or _eventStore is null
                if (!reconnect && !(_eventStore is null))
                    return;

                try
                {
                    _logger.LogDebug("disposing of last EventStore-Connection");

                    if (!(_eventStore is null))
                    {
                        _eventStore.Connected -= OnEventStoreConnected;
                        _eventStore.Disconnected -= OnEventStoreDisconnected;
                        _eventStore.Reconnecting -= OnEventStoreReconnecting;
                        _eventSubscription?.Dispose();
                    }

                    _eventStore?.Dispose();
                    _eventStore = null;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "could not cleanly dispose of last EventStore-Connection - this will likely cause problems in the future");
                }

                _logger.LogDebug("using current config to connect to EventStore");

                var configuration = _configuration.Get<ConfigServiceConfiguration>();
                _eventStoreConfiguration = configuration.EventStoreConnection;

                _logger.LogInformation($"connecting to '{configuration.EventStoreConnection.Uri}' " +
                                       $"using connectionName: '{configuration.EventStoreConnection.ConnectionName}'");

                try
                {
                    _eventStore = EventStoreConnection.Create($"ConnectTo={configuration.EventStoreConnection.Uri}",
                                                              ConnectionSettings.Create()
                                                                                .PerformOnAnyNode()
                                                                                .PreferRandomNode()
                                                                                .KeepReconnecting()
                                                                                .LimitRetriesForOperationTo(6)
                                                                                .UseCustomLogger(_eventStoreLogger),
                                                              configuration.EventStoreConnection.ConnectionName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error in EventStore Connection-Settings");
                    throw;
                }

                _eventStore.Connected += OnEventStoreConnected;
                _eventStore.Disconnected += OnEventStoreDisconnected;
                _eventStore.Reconnecting += OnEventStoreReconnecting;

                try
                {
                    _eventStore.ConnectAsync().RunSync();
                    _eventSubscription = _eventStore.SubscribeToAllAsync(true, OnEventAppeared).RunSync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "couldn't connect to EventStore");
                }
            }
        }

        private void OnConfigurationChanged() => Connect(true);

        private Task OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
        {
            EventAppeared?.Invoke(this, (Subscription: subscription, ResolvedEvent: resolvedEvent));
            return Task.CompletedTask;
        }

        private void OnEventStoreConnected(object sender, ClientConnectionEventArgs args)
        {
            _metrics.Measure.Counter.Increment(KnownMetrics.EventStoreConnected);
            ConnectionState = ConnectionState.Connected;
        }

        private void OnEventStoreDisconnected(object sender, ClientConnectionEventArgs args)
        {
            _metrics.Measure.Counter.Increment(KnownMetrics.EventStoreDisconnected);
            ConnectionState = ConnectionState.Disconnected;
        }

        private void OnEventStoreReconnecting(object sender, ClientReconnectingEventArgs args)
        {
            _metrics.Measure.Counter.Increment(KnownMetrics.EventStoreReconnected);
            ConnectionState = ConnectionState.Reconnecting;
        }

        private (byte[] Data, byte[] Metadata) SerializeDomainEvent(DomainEvent domainEvent)
            => ((IDomainEventConverter) _provider.GetService(typeof(IDomainEventConverter<>).MakeGenericType(domainEvent.GetType())))
                .Serialize(domainEvent);
    }
}