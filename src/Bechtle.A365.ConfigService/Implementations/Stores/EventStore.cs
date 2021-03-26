using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IEventStore" />
    public sealed class EventStore : IEventStore
    {
        private readonly object _connectionLock;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IOptionsMonitor<EventStoreConnectionConfiguration> _eventStoreConfiguration;
        private readonly ILoggerFactory _eventStoreLogger;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;

        private EventStoreClient _eventStore;
        private StreamSubscription _eventSubscription;

        /// <inheritdoc cref="EventStore" />
        /// <param name="logger"></param>
        /// <param name="eventStoreLogger"></param>
        /// <param name="provider"></param>
        /// <param name="eventDeserializer"></param>
        /// <param name="eventStoreConfiguration"></param>
        public EventStore(ILogger<EventStore> logger,
                          ILoggerFactory eventStoreLogger,
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

            var result = (await _eventStore.ReadStreamAsync(Direction.Backwards,
                                                            _eventStoreConfiguration.CurrentValue.Stream,
                                                            StreamPosition.End,
                                                            1,
                                                            resolveLinkTos: true)
                                           .FirstOrDefaultAsync())
                         .Event
                         .EventNumber;

            return result.ToInt64();
        }

        /// <inheritdoc />
        public Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
                                         StreamDirection direction = StreamDirection.Forwards,
                                         long startIndex = -1)
            => ReplayEventsAsStream(null, streamProcessor, direction, startIndex);

        /// <inheritdoc />
        public async Task ReplayEventsAsStream(Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool> streamFilter,
                                               Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool> streamProcessor,
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
            }, direction, startIndex);
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

                return new EventData(Uuid.NewUuid(),
                                     e.EventType,
                                     data,
                                     metadata);
            }).ToList();

            foreach (var item in eventData)
                _logger.LogInformation("sending " +
                                       $"EventId: '{item.EventId}'; " +
                                       $"Type: '{item.Type}'; " +
                                       $"ContentType: '{item.ContentType}'; " +
                                       $"Data: {item.Data.Length} bytes; " +
                                       $"Metadata: {item.Metadata.Length} bytes;");

            var result = await _eventStore.AppendToStreamAsync(_eventStoreConfiguration.CurrentValue.Stream,
                                                               StreamState.Any,
                                                               eventData);

            foreach (var item in domainEvents)
                KnownMetrics.EventsWritten.WithLabels(item.EventType).Inc();

            _logger.LogDebug($"sent {domainEvents.Count} events '{eventList}': " +
                             $"NextExpectedStreamRevision: {result.NextExpectedStreamRevision}; " +
                             $"CommitPosition: {result.LogPosition.CommitPosition}; " +
                             $"PreparePosition: {result.LogPosition.PreparePosition};");

            return result.NextExpectedStreamRevision.ToInt64();
        }

        private void Connect(bool reconnect = false)
        {
            // use lock to prevent multiple simultaneous calls to Connect causing multiple connections
            lock (_connectionLock)
            {
                // only continue if either reconnect or _eventStore is null
                if (!reconnect
                    && !(_eventStore is null)
                    && !(_eventSubscription is null))
                    return;

                try
                {
                    _logger.LogDebug("disposing of last EventStore-Connection");

                    if (!(_eventStore is null))
                    {
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
                    _eventStore = new EventStoreClient(new EventStoreClientSettings
                    {
                        ConnectionName = MakeConnectionName(),
                        LoggerFactory = _eventStoreLogger,
                        ConnectivitySettings = new EventStoreClientConnectivitySettings
                        {
                            Address = new Uri(_eventStoreConfiguration.CurrentValue.Uri),
                            NodePreference = NodePreference.Random
                        }
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error in EventStore Connection-Settings");
                    throw;
                }

                try
                {
                    _eventSubscription = _eventStore.SubscribeToAllAsync(OnEventAppeared, true).RunSync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "couldn't connect to EventStore");
                }
            }
        }

        private string MakeConnectionName()
            => $"{_eventStoreConfiguration.CurrentValue.ConnectionName}-{Environment.UserDomainName}\\{Environment.UserName}@{Environment.MachineName}";

        private Task OnEventAppeared(StreamSubscription subscription, ResolvedEvent resolvedEvent, CancellationToken cancellationToken)
        {
            var storeSubscription = new StoreSubscription
            {
                LastEventNumber = resolvedEvent.Event.EventNumber.ToInt64(),
                StreamId = resolvedEvent.Event.EventStreamId
            };

            var storedEvent = new StoredEvent
            {
                EventId = resolvedEvent.Event.EventId,
                Data = resolvedEvent.Event.Data,
                Metadata = resolvedEvent.Event.Metadata,
                EventType = resolvedEvent.Event.EventType,
                EventNumber = resolvedEvent.Event.EventNumber.ToInt64(),
                UtcTime = resolvedEvent.Event.Created.ToUniversalTime()
            };

            EventAppeared?.Invoke(this, (Subscription: storeSubscription, StoredEvent: storedEvent));
            return Task.CompletedTask;
        }

        private (ReadOnlyMemory<byte> Data, ReadOnlyMemory<byte> Metadata) SerializeDomainEvent(DomainEvent domainEvent)
            => ((IDomainEventConverter) _provider.GetService(typeof(IDomainEventConverter<>).MakeGenericType(domainEvent.GetType())))
                .Serialize(domainEvent);

        /// <summary>
        ///     read n-events at a time from the configured EventStore.
        ///     for each item, execute <paramref name="streamProcessor" />, until no more items are available or <paramref name="streamProcessor" /> returns false
        /// </summary>
        /// <param name="streamProcessor"></param>
        /// <param name="direction"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private async Task StreamEvents(Func<StoredEvent, bool> streamProcessor,
                                        StreamDirection direction = StreamDirection.Forwards,
                                        long startIndex = -1)
        {
            var streamOrigin = direction == StreamDirection.Forwards
                                   ? StreamPosition.Start
                                   : StreamPosition.End;

            // use either the given start-position (if startIndex positive or 0), or the Beginning (depending on Direction)
            var currentPosition = startIndex >= 0
                                      ? StreamPosition.FromInt64(startIndex)
                                      : streamOrigin;

            var streamName = _eventStoreConfiguration.CurrentValue.Stream;

            _logger.LogDebug($"replaying all events from stream '{streamName}' " +
                             (direction == StreamDirection.Forwards ? "forwards from the start" : "backwards from the end"));

            var stream = _eventStore.ReadStreamAsync(direction == StreamDirection.Forwards ? Direction.Forwards : Direction.Backwards,
                                                     _eventStoreConfiguration.CurrentValue.Stream,
                                                     currentPosition,
                                                     resolveLinkTos: true);

            await foreach (var resolvedEvent in stream)
            {
                KnownMetrics.EventsRead.Inc();
                var payload = new StoredEvent
                {
                    EventId = resolvedEvent.Event.EventId,
                    Data = resolvedEvent.Event.Data,
                    Metadata = resolvedEvent.Event.Metadata,
                    EventType = resolvedEvent.Event.EventType,
                    EventNumber = resolvedEvent.Event.EventNumber.ToInt64(),
                    UtcTime = resolvedEvent.Event.Created.ToUniversalTime()
                };
                if (!streamProcessor.Invoke(payload))
                    break;
            }
        }
    }
}