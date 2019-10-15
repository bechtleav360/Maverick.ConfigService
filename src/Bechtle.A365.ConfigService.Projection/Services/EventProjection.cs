using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.ConfigService.Projection.Extensions;
using Bechtle.A365.ConfigService.Projection.Metrics;
using Bechtle.A365.ConfigService.Projection.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc />
    /// <summary>
    ///     worker that processes events from a queue (<see cref="EventConverter"/>)
    /// </summary>
    public class EventProjection : HostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IMetricService _metricService;
        private readonly IMetrics _metrics;
        private readonly IEventQueue _eventQueue;

        public EventProjection(IServiceProvider serviceProvider,
                               IConfiguration configuration,
                               ILogger<EventProjection> logger,
                               IMetricService metricService,
                               IMetrics metrics,
                               IEventQueue eventQueue)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _metricService = metricService;
            _metrics = metrics;
            _eventQueue = eventQueue;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            _metricService.SetNodeId($"ConfigService.Projection@{Environment.MachineName}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_eventQueue.TryDequeue(out var projectedEvent))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        continue;
                    }

                    _metricService.SetStatus(ProjectionStatus.Projecting)
                                  .SetCurrentEvent(projectedEvent.DomainEvent,
                                                   EventProjectionResult.Undefined,
                                                   DateTime.Now,
                                                   projectedEvent.Index,
                                                   projectedEvent.Id)
                                  .Finish();

                    await Project(projectedEvent.DomainEvent,
                                  projectedEvent.Id,
                                  projectedEvent.Index);

                    _metricService.SetStatus(ProjectionStatus.Idle)
                                  .ClearCurrentEvent()
                                  .Finish();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "error while projecting DomainEvent, see previous messages for more information");
                }
            }
        }

        private async Task Project(DomainEvent domainEvent, string id, long index)
        {
            _logger.LogInformation($"projecting DomainEvent of type '{domainEvent.EventType}' / '{id}'");

            // handle all the metrics-stuff at the start so we don't have to worry about it anymore
            var transactionTags = MetricsExtensions.CreateTransactionTags("Worker: Event-Projection");
            var endpointTags = MetricsExtensions.CreateEndpointTags("Worker: Event-Projection",
                                                                    domainEvent.EventType);

            // initialize several disposable timers that will be disposed together
            // these don't really matter for functionality, but are required for the app-metrics
            using var _ = new AggregateDisposable(
                _metrics.Measure.Apdex.Track(KnownMetrics.Apdex(_configuration.GetSection("MetricsOptions:ApdexTSeconds").Get<double>()), transactionTags),
                _metrics.Measure.Apdex.Track(KnownMetrics.Apdex(_configuration.GetSection("MetricsOptions:ApdexTSeconds").Get<double>()), endpointTags),
                _metrics.Measure.Timer.Time(KnownMetrics.RequestTransactionDuration, transactionTags),
                _metrics.Measure.Timer.Time(KnownMetrics.EndpointRequestTransactionDuration, endpointTags));

            _metrics.Measure.Counter.Increment(KnownMetrics.ActiveRequestCount);

            var metadata = new ProjectedEventMetadata
            {
                Type = domainEvent.EventType,
                Start = DateTime.UtcNow,
                Index = index
            };

            using var scope = _serviceProvider.CreateScope();
            await using var context = scope.ServiceProvider.GetService<ProjectionStoreContext>();
            await using var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();
            await using var transaction = context.Database.BeginTransaction();

            _logger.LogInformation($"using transaction '{transaction.TransactionId}' for event '{domainEvent.EventType}'");

            try
            {
                _logger.LogDebug($"projecting event '{domainEvent.EventType}'");

                await ProcessDomainEvent(domainEvent, scope.ServiceProvider);

                _logger.LogDebug($"recording successful projection of event #{index} to database");

                await database.SetLatestProjectedEventId(index);

                _logger.LogInformation("saving changes made to the database...");

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"saving '{context.ChangeTracker.Entries().Count()}' changes made to the database...");

                await context.SaveChangesAsync();

                _logger.LogInformation($"committing transaction '{transaction.TransactionId}'");

                metadata.Changes = context.ChangeTracker.Entries().Count();
                transaction.Commit();
                metadata.ProjectedSuccessfully = true;

                _metrics.Measure.Counter.Increment(KnownMetrics.DatabaseUpdates, metadata.Changes);

                _logger.LogInformation($"transaction '{transaction.TransactionId}' committed");
            }
            catch (Exception e)
            {
                transaction.Rollback();
                metadata.ProjectedSuccessfully = false;

                _logger.LogCritical($"could not project domain-event of type '{domainEvent.EventType}', " +
                                    $"rolling back transaction '{transaction.TransactionId}' :{e}");

                _metrics.RegisterFailure(endpointTags, transactionTags);
            }
            finally
            {
                _metrics.Measure.Counter.Increment(KnownMetrics.EventsProjected, domainEvent.EventType);
                _metrics.Measure.Counter.Decrement(KnownMetrics.ActiveRequestCount);
                _logger.LogTrace("forcing GC.Collect...");
                metadata.End = DateTime.UtcNow;
                GC.Collect();
            }

            await database.AppendProjectedEventMetadata(metadata);
        }

        private async Task ProcessDomainEvent(DomainEvent domainEvent, IServiceProvider provider)
        {
            // inner function to not clutter this class any more
            Task HandleDomainEvent<T>(T @event) where T : DomainEvent => provider.GetService<IDomainEventHandler<T>>()
                                                                                 .HandleDomainEvent(@event);

            switch (domainEvent)
            {
                case null:
                    throw new ArgumentNullException(nameof(domainEvent));

                case ConfigurationBuilt configurationBuilt:
                    await HandleDomainEvent(configurationBuilt);
                    break;

                case DefaultEnvironmentCreated defaultEnvironmentCreated:
                    await HandleDomainEvent(defaultEnvironmentCreated);
                    break;

                case EnvironmentCreated environmentCreated:
                    await HandleDomainEvent(environmentCreated);
                    break;

                case EnvironmentDeleted environmentDeleted:
                    await HandleDomainEvent(environmentDeleted);
                    break;

                case EnvironmentKeysModified environmentKeyModified:
                    await HandleDomainEvent(environmentKeyModified);
                    break;

                case EnvironmentKeysImported environmentKeysImported:
                    await HandleDomainEvent(environmentKeysImported);
                    break;

                case StructureCreated structureCreated:
                    await HandleDomainEvent(structureCreated);
                    break;

                case StructureDeleted structureDeleted:
                    await HandleDomainEvent(structureDeleted);
                    break;

                case StructureVariablesModified structureVariablesModified:
                    await HandleDomainEvent(structureVariablesModified);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainEvent));
            }
        }
    }
}