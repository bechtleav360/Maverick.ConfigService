using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
// god-damn-it fuck you 'EventStore' for creating 'ILogger' when that is essentially a core component of the eco-system
using ESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IEventStore" />
    public sealed class EventStore : IEventStore
    {
        private readonly object _connectionLock;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IOptionsMonitor<EventStoreConnectionConfiguration> _eventStoreConfiguration;
        private readonly ESLogger _eventStoreLogger;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;

        private IEventStoreConnection _eventStore;
        private EventStoreSubscription _eventSubscription;

        /// <inheritdoc cref="EventStore" />
        /// <param name="logger"></param>
        /// <param name="eventStoreLogger"></param>
        /// <param name="provider"></param>
        /// <param name="eventDeserializer"></param>
        /// <param name="eventStoreConfiguration"></param>
        public EventStore(ILogger<EventStore> logger,
                          ESLogger eventStoreLogger,
                          IServiceProvider provider,
                          IEventDeserializer eventDeserializer,
                          IOptionsMonitor<EventStoreConnectionConfiguration> eventStoreConfiguration)
        {
            _connectionLock = new object();
            _logger = logger;
            _eventStoreLogger = eventStoreLogger;
            _provider = provider;
            _eventDeserializer = eventDeserializer;
            _eventStoreConfiguration = eventStoreConfiguration;

            _eventStoreConfiguration.OnChange(_ => Connect(true));
        }

        /// <inheritdoc cref="Interfaces.ConnectionState" />
        public static ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

        /// <inheritdoc />
        public event EventHandler<(StoreSubscription Subscription, StoredEvent StoredEvent)> EventAppeared;

        /// <inheritdoc />
        public void Dispose()
        {
            _eventSubscription?.Dispose();
            _eventStore?.Dispose();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask(Task.CompletedTask);
        }

        /// <inheritdoc />
        public async Task<long> GetCurrentEventNumber()
        {
            // connect if we're not already connected
            Connect();

            var result = await _eventStore.ReadStreamEventsBackwardAsync(_eventStoreConfiguration.CurrentValue.Stream, StreamPosition.End, 1, true);

            return result.LastEventNumber;
        }

        /// <inheritdoc />
        public Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                         int readSize = 128,
                                         StreamDirection direction = StreamDirection.Forwards,
                                         long startIndex = -1)
            => ReplayEventsAsStream(null, streamProcessor, readSize, direction, startIndex);

        /// <inheritdoc />
        public async Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool> streamFilter,
                                               Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                               int readSize = 128,
                                               StreamDirection direction = StreamDirection.Forwards,
                                               long startIndex = -1)
        {
            // connect if we're not already connected
            Connect();

            if (streamProcessor is null)
                return;

            await StreamEvents(storedEvent =>
            {
                // initialize to default value if we can't deserialize successfully
                if (!_eventDeserializer.ToMetadata(storedEvent, out var metadata))
                    metadata = new DomainEventMetadata();

                // skip this event entirely if filter evaluates to false
                if (streamFilter != null && !streamFilter((StoredEvent: storedEvent, Metadata: metadata)))
                {
                    KnownMetrics.EventsFiltered.WithLabels(storedEvent.EventType).Inc();
                    return true;
                }

                if (!_eventDeserializer.ToDomainEvent(storedEvent, out var domainEvent))
                {
                    _logger.LogWarning($"event {storedEvent.EventId}#{storedEvent.EventNumber} could not be deserialized");
                    return true;
                }

                KnownMetrics.EventsStreamed.WithLabels(domainEvent.EventType).Inc();

                // stop streaming events once streamProcessor returns false
                return streamProcessor.Invoke((storedEvent, domainEvent));
            }, readSize, direction, startIndex);
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

            var result = await _eventStore.AppendToStreamAsync(_eventStoreConfiguration.CurrentValue.Stream,
                                                               ExpectedVersion.Any,
                                                               eventData);

            foreach (var item in domainEvents)
                KnownMetrics.EventsWritten.WithLabels(item.EventType).Inc();

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
                if (!reconnect
                    && !(_eventStore is null)
                    && !(_eventSubscription is null)
                    && ConnectionState == ConnectionState.Connected)
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

                _logger.LogInformation($"connecting to '{_eventStoreConfiguration.CurrentValue.Uri}' " +
                                       $"using connectionName: '{_eventStoreConfiguration.CurrentValue.ConnectionName}'");

                try
                {
                    _eventStore = EventStoreConnection.Create($"ConnectTo={_eventStoreConfiguration.CurrentValue.Uri}",
                                                              ConnectionSettings.Create()
                                                                                .PerformOnAnyNode()
                                                                                .PreferRandomNode()
                                                                                .KeepReconnecting()
                                                                                .LimitRetriesForOperationTo(10)
                                                                                .LimitConcurrentOperationsTo(1)
                                                                                .SetOperationTimeoutTo(TimeSpan.FromSeconds(30))
                                                                                .SetReconnectionDelayTo(TimeSpan.FromSeconds(1))
                                                                                .SetHeartbeatTimeout(TimeSpan.FromSeconds(30))
                                                                                .SetHeartbeatInterval(TimeSpan.FromSeconds(60))
                                                                                .UseCustomLogger(_eventStoreLogger),
                                                              MakeConnectionName());
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

        private string MakeConnectionName()
            => $"{_eventStoreConfiguration.CurrentValue.ConnectionName}-{Environment.UserDomainName}\\{Environment.UserName}@{Environment.MachineName}";

        private Task OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
        {
            var storeSubscription = new StoreSubscription
            {
                LastEventNumber = subscription.LastEventNumber,
                StreamId = subscription.StreamId
            };

            var storedEvent = new StoredEvent
            {
                EventId = resolvedEvent.Event.EventId,
                Data = resolvedEvent.Event.Data,
                Metadata = resolvedEvent.Event.Metadata,
                EventType = resolvedEvent.Event.EventType,
                EventNumber = resolvedEvent.Event.EventNumber,
                UtcTime = resolvedEvent.Event.Created.ToUniversalTime()
            };

            EventAppeared?.Invoke(this, (Subscription: storeSubscription, StoredEvent: storedEvent));
            return Task.CompletedTask;
        }

        private void OnEventStoreConnected(object sender, ClientConnectionEventArgs args)
        {
            KnownMetrics.EventStoreConnected.Inc();
            ConnectionState = ConnectionState.Connected;
        }

        private void OnEventStoreDisconnected(object sender, ClientConnectionEventArgs args)
        {
            KnownMetrics.EventStoreDisconnected.Inc();
            ConnectionState = ConnectionState.Disconnected;
            Connect();
        }

        private void OnEventStoreReconnecting(object sender, ClientReconnectingEventArgs args)
        {
            KnownMetrics.EventStoreReconnected.Inc();
            ConnectionState = ConnectionState.Reconnecting;
        }

        private (byte[] Data, byte[] Metadata) SerializeDomainEvent(DomainEvent domainEvent)
            => ((IDomainEventConverter) _provider.GetService(typeof(IDomainEventConverter<>).MakeGenericType(domainEvent.GetType())))
                .Serialize(domainEvent);

        /// <summary>
        ///     read n-events at a time from the configured EventStore.
        ///     for each item, execute <paramref name="streamProcessor" />, until no more items are available or <paramref name="streamProcessor" /> returns false
        /// </summary>
        /// <param name="streamProcessor"></param>
        /// <param name="readSize"></param>
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private async Task StreamEvents(Func<StoredEvent, bool> streamProcessor,
                                        int readSize = 64,
                                        StreamDirection direction = StreamDirection.Forwards,
                                        long startIndex = -1)
        {
            // readSize must be below 4096
            // seems to cause problems with increasing number of items
            // will be limited to 128 for now
            readSize = Math.Min(readSize, 256);

            var streamOrigin = direction == StreamDirection.Forwards
                                   ? StreamPosition.Start
                                   : StreamPosition.End;

            // use either the given start-position (if startIndex positive or 0), or the Beginning (depending on Direction)
            var currentPosition = startIndex >= 0
                                      ? startIndex
                                      : streamOrigin;

            var stream = _eventStoreConfiguration.CurrentValue.Stream;
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

                KnownMetrics.EventsRead.Inc(slice.Events.Length);

                if (slice.Events
                         .Select(e => new StoredEvent
                         {
                             EventId = e.Event.EventId,
                             Data = e.Event.Data,
                             Metadata = e.Event.Metadata,
                             EventType = e.Event.EventType,
                             EventNumber = e.Event.EventNumber,
                             UtcTime = e.Event.Created.ToUniversalTime()
                         })
                         // if any streamProcessor returns false, stop streaming
                         .Any(storedEvent => !streamProcessor(storedEvent)))
                    return;

                currentPosition = slice.NextEventNumber;
                continueReading = !slice.IsEndOfStream;
            } while (continueReading);
        }
    }
}