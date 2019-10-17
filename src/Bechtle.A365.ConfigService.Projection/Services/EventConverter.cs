using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Bechtle.A365.ConfigService.Projection.Metrics;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using Polly;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc />
    /// <summary>
    ///     convert incoming events so they can be projected by <see cref="EventProjection" />
    /// </summary>
    public class EventConverter : HostedService
    {
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IEventQueue _eventQueue;
        private readonly ILogger<EventConverter> _logger;
        private readonly IMetrics _metrics;
        private readonly IMetricService _metricService;
        private readonly IServiceProvider _serviceProvider;

        // not readonly because .Close() disposes the object
        // and then we need a new one when .Connect()ing again
        private IEventStoreConnection _eventStore;

        public EventConverter(IServiceProvider serviceProvider,
                              ILogger<EventConverter> logger,
                              IEventDeserializer eventDeserializer,
                              IMetricService metricService,
                              IMetrics metrics,
                              IEventQueue eventQueue)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _eventDeserializer = eventDeserializer;
            _metricService = metricService;
            _metrics = metrics;
            _eventQueue = eventQueue;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            await Policy.Handle<Exception>()
                        .WaitAndRetryForeverAsync(i =>
                        {
                            var maxTimeout = TimeSpan.FromMinutes(1);
                            var desiredTimeout = TimeSpan.FromSeconds(5).Multiply(i);

                            return desiredTimeout.TotalSeconds > maxTimeout.TotalSeconds
                                       ? maxTimeout
                                       : desiredTimeout;
                        })
                        .ExecuteAsync(Connect);
        }

        private async Task Connect()
        {
            _logger.LogInformation("connecting to EventStore/Db to prepare DomainEvents");
            _logger.LogDebug("retrieving current configuration");

            var configuration = _serviceProvider.GetService<IConfiguration>();
            var config = configuration.Get<ProjectionConfiguration>();

            long? latestEvent;
            using (var scope = _serviceProvider.CreateScope())
            {
                _logger.LogDebug("trying to open connection to Database");

                var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();
                await database.Connect();

                _logger.LogDebug("connected to database, retrieving latest projected event-id");

                latestEvent = await database.GetLatestProjectedEventId();

                _logger.LogDebug($"latest event-id retrieved: '{latestEvent}'");
            }

            _logger.LogDebug("trying to open connection to EventStore");

            _eventStore = _serviceProvider.GetService<IEventStoreConnection>();
            await _eventStore.ConnectAsync();

            _logger.LogDebug($"EventStore connected, subscribing to stream '{config.EventStoreConnection.Stream}'");

            _eventStore.SubscribeToStreamFrom(
                config.EventStoreConnection.Stream,
                latestEvent,
                new CatchUpSubscriptionSettings(config.EventStoreConnection.MaxLiveQueueSize,
                                                config.EventStoreConnection.ReadBatchSize,
                                                false,
                                                true,
                                                config.EventStoreConnection.Stream),
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped);

            // do this last, because we only care about updates if we actually manage to complete the connection-process
            ChangeToken.OnChange(configuration.GetReloadToken, OnConfigurationChanged);
        }

        private void Disconnect()
        {
            _logger.LogInformation("closing connection to EventStore");
            _eventStore?.Close();
            _eventStore?.Dispose();
            _eventStore = null;
        }

        private void EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            var transactionTags = MetricsExtensions.CreateTransactionTags("Worker: Event-Conversion");
            var endpointTags = MetricsExtensions.CreateEndpointTags("Worker: Event-Conversion",
                                                                    resolvedEvent.Event.EventType);

            _metrics.Measure.Histogram.Update(KnownMetrics.PostRequestSizeHistogram,
                                              resolvedEvent.Event.Data.Length
                                              + resolvedEvent.Event.Metadata.Length);

            using (_metrics.Measure.Timer.Time(KnownMetrics.RequestTransactionDuration, transactionTags))
            using (_metrics.Measure.Timer.Time(KnownMetrics.EndpointRequestTransactionDuration, endpointTags))
            {
                try
                {
                    _metrics.Measure.Counter.Increment(KnownMetrics.ActiveRequestCount);

                    _logger.LogInformation($"Stream: {resolvedEvent.OriginalStreamId}#{resolvedEvent.OriginalEventNumber}; " +
                                           $"EventId: {resolvedEvent.OriginalEvent.EventId}; " +
                                           $"EventType: {resolvedEvent.OriginalEvent.EventType}; " +
                                           $"Created: {resolvedEvent.OriginalEvent.Created}; " +
                                           $"IsJson: {resolvedEvent.OriginalEvent.IsJson}; " +
                                           $"Data: {resolvedEvent.OriginalEvent.Data.Length} bytes; " +
                                           $"Metadata: {resolvedEvent.OriginalEvent.Metadata.Length} bytes;");

                    if (!_eventDeserializer.ToDomainEvent(resolvedEvent, out var domainEvent))
                    {
                        var e = new Exception("unable to deserialize to DomainEvent");
                        e.Data["resolvedEvent"] = resolvedEvent;
                        e.Data["json"] = JsonConvert.SerializeObject(resolvedEvent);

                        throw e;
                    }

                    if (!_eventQueue.TryEnqueue(new ProjectedEvent
                    {
                        DomainEvent = domainEvent,
                        Index = resolvedEvent.OriginalEventNumber,
                        Id = $"{resolvedEvent.OriginalStreamId}#{resolvedEvent.OriginalEventNumber};{resolvedEvent.OriginalEvent.EventId:D}"
                    }))
                    {
                        var e = new Exception("unable to add DomainEvent to queue");
                        e.Data["domainEvent"] = domainEvent;
                        e.Data["json"] = JsonConvert.SerializeObject(domainEvent);

                        throw e;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "could not convert ResolvedEvent from EventStore to DomainEvent - " +
                                        "invariance of stream not given anymore, closing stream");

                    _metrics.RegisterFailure(endpointTags, transactionTags);

                    Disconnect();
                }
                finally
                {
                    _metrics.Measure.Counter.Increment(KnownMetrics.EventsConverted, resolvedEvent.Event.EventType);
                    _metrics.Measure.Counter.Decrement(KnownMetrics.ActiveRequestCount);
                }
            }
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription subscription)
        {
            _logger.LogInformation($"subscription to '{subscription.SubscriptionName}' opened");
            _metricService.SetEventStoreConnected(true).Finish();
        }

        private void OnConfigurationChanged()
        {
            Disconnect();
            Connect().RunSync();
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception)
        {
            _logger.LogCritical($"subscription '{subscription.SubscriptionName}' dropped for reason: {reason}; exception {exception}");
            _metricService.SetEventStoreConnected(false).Finish();
        }
    }
}