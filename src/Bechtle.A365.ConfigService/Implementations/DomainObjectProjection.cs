using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Exceptions;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.Services.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Component that receives
    /// </summary>
    public class DomainObjectProjection : EventSubscriptionBase
    {
        private readonly ProjectionCacheCompatibleCheck _cacheHealthCheck;
        private readonly ProjectionStatusCheck _projectionStatus;
        private readonly IOptions<EventStoreConnectionConfiguration> _configuration;
        private readonly ILogger<DomainObjectProjection> _logger;
        private readonly IDomainObjectStore _objectStore;
        private readonly DomainEventProjectionCheck _projectionHealthCheck;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectProjection" />
        /// </summary>
        /// <param name="eventStore">EventStore that handles the underlying subscription</param>
        /// <param name="objectStore">store for the projected DomainObjects</param>
        /// <param name="serviceProvider">serviceProvider used to retrieve components for compiling configurations</param>
        /// <param name="configuration">options used to configure the subscription</param>
        /// <param name="projectionHealthCheck">associated Health-Check that reports the current status of this component</param>
        /// <param name="cacheHealthCheck">
        ///     health-check associated with <see cref="ProjectionCacheCleanupService" />. This Service will wait until the health-check is ready
        /// </param>
        /// <param name="projectionStatus">associated Status-Check that reports the current status of this component</param>
        /// <param name="logger">logger to write information to</param>
        public DomainObjectProjection(
            IEventStore eventStore,
            IDomainObjectStore objectStore,
            IServiceProvider serviceProvider,
            IOptions<EventStoreConnectionConfiguration> configuration,
            DomainEventProjectionCheck projectionHealthCheck,
            ProjectionCacheCompatibleCheck cacheHealthCheck,
            ProjectionStatusCheck projectionStatus,
            ILogger<DomainObjectProjection> logger) : base(eventStore)
        {
            _objectStore = objectStore;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _projectionHealthCheck = projectionHealthCheck;
            _cacheHealthCheck = cacheHealthCheck;
            _projectionStatus = projectionStatus;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override void ConfigureStreamSubscription(IStreamSubscriptionBuilder subscriptionBuilder)
        {
            var lastProjectedEventId = Guid.Empty;
            long lastProjectedEventNumber = -1;
            try
            {
                IResult<(Guid, long)> result = _objectStore.GetProjectedVersion().Result;
                if (result.IsError)
                {
                    _logger.LogWarning("unable to tell which event was last projected, starting from scratch");
                }
                else
                {
                    _logger.LogInformation(
                        "starting DomainEvent-Projection at event {LastProjectedEvent}",
                        lastProjectedEventNumber);
                    (lastProjectedEventId, lastProjectedEventNumber) = result.Data;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "error while checking which event was last projected, starting from scratch");
            }

            subscriptionBuilder.ToStream(_configuration.Value.Stream);

            // having projected event-0 is a valid possibility
            if (lastProjectedEventNumber < 0)
            {
                subscriptionBuilder.FromStart();
            }
            else
            {
                // we're losing half the amount of events we could be projecting
                // but the ConfigService isn't meant to handle either
                // 18.446.744.073.709.551.615 or 9.223.372.036.854.775.807 events, so cutting off half our range seems fine
                subscriptionBuilder.FromEvent((ulong)lastProjectedEventNumber);
            }

            _projectionStatus.SetCurrentlyProjecting(
                new StreamedEventHeader
                {
                    Created = DateTime.UnixEpoch,
                    EventId = lastProjectedEventId,
                    EventNumber = (ulong)lastProjectedEventNumber,
                    EventStreamId = string.Empty,
                    EventType = string.Empty,
                    StreamId = string.Empty
                });

            _projectionHealthCheck.SetReady();
        }

        /// <summary>
        ///     Configure and start this Subscription, after <see cref="_cacheHealthCheck" /> is ready
        /// </summary>
        /// <param name="stoppingToken">token to stop this subscription with</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // stop normal subscription-flow until cache is killed / ready
            _logger.LogInformation("delaying subscription until the caches compatibility is checked");

            while (!_cacheHealthCheck.IsReady)
            {
                _logger.LogTrace(
                    "waiting for cache-compatibility-check to be ready: '{CompatibilityChecked}'",
                    _cacheHealthCheck.IsReady);
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }

            // back to our normal flow
            await base.ExecuteAsync(stoppingToken);
        }

        /// <inheritdoc />
        protected override async Task OnDomainEventReceived(
            StreamedEventHeader? eventHeader,
            IDomainEvent? domainEvent)
        {
            _projectionHealthCheck.SetReady();

            if (eventHeader is null)
            {
                _logger.LogWarning("unable to project domainEvent, no header provided - likely serialization problem");
                return;
            }

            if (domainEvent is null)
            {
                _logger.LogWarning(
                    "unable to project domainEvent #{EventNumber} with id {EventId} of type {EventType}, "
                    + "no body provided - likely serialization problem",
                    eventHeader.EventNumber,
                    eventHeader.EventId,
                    eventHeader.EventType);
                return;
            }

            try
            {
                _logger.LogInformation(
                    "projecting domainEvent #{EventNumber} with id {EventId} of type {EventType}",
                    eventHeader.EventNumber,
                    eventHeader.EventId,
                    eventHeader.EventType);

                _projectionStatus.SetCurrentlyProjecting(eventHeader);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                using IServiceScope? scope = _serviceProvider.CreateScope();
                IServiceProvider? services = scope.ServiceProvider;

                // @TODO: acquire instance of IDomainEventProjection<TDomainEvent>
                Task? task = domainEvent switch
                {
                    IDomainEvent<ConfigurationBuilt> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<DefaultEnvironmentCreated> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentCreated> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentDeleted> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayersModified> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerCreated> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerDeleted> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerCopied> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerTagsChanged> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerKeysImported> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<EnvironmentLayerKeysModified> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<StructureCreated> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<StructureDeleted> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    IDomainEvent<StructureVariablesModified> e => ForwardDomainEventToProjections(eventHeader, e, services),
                    _ => null
                };

                if (task is null)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "domainEvent #{EventNumber} of type {DomainEvent} was not projected - missing event-handler",
                        eventHeader.EventNumber,
                        eventHeader.EventType);
                    return;
                }

                await task;

                await _objectStore.SetProjectedVersion(
                    eventHeader.EventId.ToString("D"),
                    (long)eventHeader.EventNumber,
                    eventHeader.EventType);

                stopwatch.Stop();

                _projectionStatus.SetDoneProjecting(eventHeader);

                KnownMetrics.ProjectionTime
                            .WithLabels(eventHeader.EventType)
                            .Observe(stopwatch.Elapsed.TotalSeconds);

                KnownMetrics.EventsProjected
                            .WithLabels(eventHeader.EventType)
                            .Inc();
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "error while projecting domainEvent {DomainEventId}#{DomainEventNumber} of type {DomainEventType}",
                    eventHeader.EventId,
                    eventHeader.EventNumber,
                    eventHeader.EventType);
                _projectionStatus.SetErrorWhileProjecting(eventHeader, e);
                throw;
            }
        }

        private async Task ForwardDomainEventToProjections<TDomainEvent>(
            StreamedEventHeader eventHeader,
            IDomainEvent<TDomainEvent> domainEvent,
            IServiceProvider services)
        {
            List<IDomainEventProjection<TDomainEvent>> handlers = services.GetServices<IDomainEventProjection<TDomainEvent>>()
                                                                          .ToList();

            if (!handlers.Any())
            {
                _logger.LogError("no handlers for domain-event '{DomainEventType}' registered", typeof(TDomainEvent).Name);
                throw new MissingHandlerException(typeof(TDomainEvent).Name);
            }

            foreach (var handler in handlers)
            {
                string handlerName = handler.GetType().Name;
                try
                {
                    _logger.LogDebug(
                        "forwarding event #{EventNumber} to handler {HandlerName}",
                        eventHeader.EventNumber,
                        handlerName);
                    await handler.ProjectChanges(eventHeader, domainEvent);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "error while forwarding event #{EventNumber} to handler {HandlerName}",
                        eventHeader.EventNumber,
                        handlerName);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionDropped(string streamId, string streamName, Exception exception)
        {
            _projectionHealthCheck.SetReady(false);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnSubscriptionOpened()
        {
            _projectionHealthCheck.SetReady();
            return Task.CompletedTask;
        }
    }
}
