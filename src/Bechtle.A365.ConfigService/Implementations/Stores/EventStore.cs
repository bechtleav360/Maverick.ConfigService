using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Exceptions;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc cref="IEventStore" />
    public sealed class EventStore : IEventStore
    {
        /// <summary>
        ///     The current default 'Append-Size' / 'Message-Size' when writing new events to ES
        /// </summary>
        private const long DefaultMaxAppendSize = 1 * 1024 * 1024;

        private readonly object _connectionLock;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IOptionsMonitor<EventStoreConnectionConfiguration> _eventStoreConfiguration;
        private readonly ILoggerFactory _eventStoreLogger;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;

        private EventStoreClient _eventStore;
        private StreamSubscription _eventSubscription;
        private List<EventStoreOption> _eventStoreOptions;

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
            _eventSubscription = null;
            _eventStore?.Dispose();
            _eventStore = null;
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

        private long MaxAppendSize
        {
            get
            {
                string option = _eventStoreOptions?.FirstOrDefault(
                                                      o => o.Name.Equals(
                                                          "MaxAppendSize",
                                                          StringComparison.OrdinalIgnoreCase))
                                                  ?.Value;

                return string.IsNullOrWhiteSpace(option)
                       && long.TryParse(option, out long retVal)
                           ? retVal
                           : DefaultMaxAppendSize;
            }
        }

        /// <inheritdoc />
        public async Task<long> WriteEvents(IList<DomainEvent> domainEvents)
        {
            // connect if we're not already connected
            Connect();

            if (_eventStoreOptions is null)
                _eventStoreOptions = await GetEventStoreOptionsAsync();

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

            // sum size of all events to compute total message-size
            var writeSize = eventData.Sum(
                e => e.Data.Length
                     + e.Metadata.Length
                     + e.Type.Length
                     + e.ContentType.Length
                     // size of e.EventId / UUID
                     + 128);

            // calculate Property once
            var maxAppendSize = MaxAppendSize;

            if (writeSize >= maxAppendSize)
                throw new InvalidMessageSizeException(writeSize, maxAppendSize);

            foreach (var item in eventData)
                _logger.LogInformation("sending " +
                                       $"EventId: '{item.EventId}'; " +
                                       $"Type: '{item.Type}'; " +
                                       $"ContentType: '{item.ContentType}'; " +
                                       $"Data: {item.Data.Length} bytes; " +
                                       $"Metadata: {item.Metadata.Length} bytes;");

            try
            {
                var result = await _eventStore.AppendToStreamAsync(
                                 _eventStoreConfiguration.CurrentValue.Stream,
                                 StreamState.Any,
                                 eventData);

                foreach (var item in domainEvents)
                    KnownMetrics.EventsWritten.WithLabels(item.EventType).Inc();

                _logger.LogDebug(
                    $"sent {domainEvents.Count} events '{eventList}': "
                    + $"NextExpectedStreamRevision: {result.NextExpectedStreamRevision}; "
                    + $"CommitPosition: {result.LogPosition.CommitPosition}; "
                    + $"PreparePosition: {result.LogPosition.PreparePosition};");

                return result.NextExpectedStreamRevision.ToInt64();
            }
            catch (MaximumAppendSizeExceededException e)
            {
                // this *should* not happen when we explicitly retrieve the MaxAppendSize-option
                // but something might still go wrong (different configs, changed configs / new instances, ...)
                _logger.LogWarning(e, "message was larger than ES allowed");
                throw new InvalidMessageSizeException(writeSize, maxAppendSize, e);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to write to EventStore");
                throw;
            }
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

                // clear cached ES-Options, so we can pull them again on the next write
                _eventStoreOptions = null;

                try
                {
                    _logger.LogDebug("disposing of last EventStore-Connection");

                    if (!(_eventStore is null))
                    {
                        _eventSubscription?.Dispose();
                        _eventSubscription = null;
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
                    var settings = EventStoreClientSettings.Create(_eventStoreConfiguration.CurrentValue.Uri);
                    settings.LoggerFactory = _eventStoreLogger;
                    settings.ConnectionName = MakeConnectionName();
                    settings.ConnectivitySettings.NodePreference = NodePreference.Follower;
                    settings.OperationOptions.TimeoutAfter = TimeSpan.FromMinutes(1);
                    _eventStore = new EventStoreClient(settings);
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
            try
            {
                // ReSharper disable once ConstantNullCoalescingCondition
                // yes, the documentation says EventRecord.Event is guaranteed to not be null and either point to the linked or original event
                // no, that guarantee does not hold up and we do get null-references on this guaranteed-to-not-be-null property
                // yes, this is bullshit
                // yes, this was expected from EventStore
                var actualEvent = resolvedEvent.Event ?? resolvedEvent.Link;

                var storeSubscription = new StoreSubscription
                {
                    LastEventNumber = actualEvent.EventNumber.ToInt64(),
                    StreamId = actualEvent.EventStreamId
                };

                var storedEvent = new StoredEvent
                {
                    EventId = actualEvent.EventId,
                    Data = actualEvent.Data,
                    Metadata = actualEvent.Metadata,
                    EventType = actualEvent.EventType,
                    EventNumber = actualEvent.EventNumber.ToInt64(),
                    UtcTime = actualEvent.Created.ToUniversalTime()
                };

                EventAppeared?.Invoke(this, (Subscription: storeSubscription, StoredEvent: storedEvent));
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "internal error while reading event");
                throw;
            }
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

            try
            {
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
            catch (StreamNotFoundException)
            {
                // stream doesn't exist yet
                // we only catch this, because there is no "good" way to check for this before reading
            }
            catch (Exception e)
            {
                // unknown exception while reading
                _logger.LogWarning(e, "unable to read events from EventStore");
            }
        }

        private async Task<List<EventStoreOption>> GetEventStoreOptionsAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var storeUri = new Uri(_eventStoreConfiguration.CurrentValue.Uri);

            Uri optionsUri = storeUri.Query.Contains("tls=true", StringComparison.OrdinalIgnoreCase)
                                 ? new Uri($"https://{storeUri.Authority}{storeUri.AbsolutePath}info/options")
                                 : new Uri($"http://{storeUri.Authority}{storeUri.AbsolutePath}info/options");

            // yes creating HttpClient is frowned upon, but we don't need it *that* often and can immediately release it
            var httpClient = new HttpClient();
            HttpResponseMessage response;

            try
            {
                response =await httpClient.GetAsync(optionsUri, cancellationToken);

                if (response is null)
                {
                    _logger.LogWarning("unable to retrieve [EventStore]/info/options");
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to retrieve [EventStore]/info/options");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonConvert.DeserializeObject<List<EventStoreOption>>(json);
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "unable to deserialize '../info/options' response");
                return null;
            }
        }

        /// <summary>
        ///     a single entry returned from [EventStore]/options
        /// </summary>
        private class EventStoreOption
        {
            /// <summary>
            ///     Name of the Option
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Value of the Option
            /// </summary>
            public string Value { get; set; }
        }
    }
}