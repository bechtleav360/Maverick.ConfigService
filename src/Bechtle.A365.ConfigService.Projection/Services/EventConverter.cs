using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc />
    /// <summary>
    ///     convert incoming events so they can be projected by <see cref="EventProjection" />
    /// </summary>
    public class EventConverter : HostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IEventQueue _eventQueue;
        private readonly ILogger<EventConverter> _logger;
        private readonly IServiceProvider _serviceProvider;

        // not readonly because .Close() disposes the object
        // and then we need a new one when .Connect()ing again
        private IEventStoreConnection _eventStore;

        public EventConverter(IServiceProvider serviceProvider,
                              ILogger<EventConverter> logger,
                              IConfiguration configuration,
                              IEventDeserializer eventDeserializer,
                              IEventQueue eventQueue)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _eventDeserializer = eventDeserializer;
            _eventQueue = eventQueue;

            ChangeToken.OnChange(_configuration.GetReloadToken, OnConfigurationChanged);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            await Connect();
        }

        private async Task Connect()
        {
            _eventStore = _serviceProvider.GetService<IEventStoreConnection>();

            var config = _configuration.Get<ProjectionConfiguration>();

            long? latestEvent;
            using (var scope = _serviceProvider.CreateScope())
            {
                var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();
                await database.Connect();
                latestEvent = await database.GetLatestProjectedEventId();
            }

            await _eventStore.ConnectAsync();

            _eventStore.SubscribeToStreamFrom(config.EventStoreConnection.Stream,
                                              latestEvent,
                                              new CatchUpSubscriptionSettings(config.EventStoreConnection.MaxLiveQueueSize,
                                                                              config.EventStoreConnection.ReadBatchSize,
                                                                              false,
                                                                              true,
                                                                              config.EventStoreConnection.Stream),
                                              EventAppeared,
                                              subscription => { _logger.LogInformation($"subscription to '{subscription.SubscriptionName}' opened"); },
                                              (subscription, reason, exception) =>
                                              {
                                                  _logger.LogCritical($"subscription '{subscription.SubscriptionName}' " +
                                                                      $"dropped for reason: {reason}; exception {exception}");
                                              });
        }

        private void Disconnect()
        {
            _logger.LogInformation("closing connection to EventStore");
            _eventStore.Close();
            _eventStore.Dispose();
            _eventStore = null;
        }

        private void EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            try
            {
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
                Disconnect();
            }
        }

        private void OnConfigurationChanged()
        {
            Disconnect();
            Connect().RunSync();
        }
    }
}