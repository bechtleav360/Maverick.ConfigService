using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
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
        private readonly ILogger<EventConverter> _logger;
        private readonly IMetricService _metricService;
        private readonly IEventQueue _eventQueue;

        public EventProjection(IServiceProvider serviceProvider,
                               ILogger<EventConverter> logger,
                               IMetricService metricService,
                               IEventQueue eventQueue)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _metricService = metricService;
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

                    var result = await Project(projectedEvent.DomainEvent,
                                               projectedEvent.Id,
                                               projectedEvent.Index);

                    _metricService.SetStatus(ProjectionStatus.Idle)
                                  .SetLastEvent(projectedEvent.DomainEvent,
                                                result ? EventProjectionResult.Success : EventProjectionResult.Failure,
                                                DateTime.Now,
                                                projectedEvent.Index,
                                                projectedEvent.Id)
                                  .ClearCurrentEvent()
                                  .Finish();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "error while projecting DomainEvent, see previous messages for more information");
                }
            }
        }

        private async Task<bool> Project(DomainEvent domainEvent, string id, long index)
        {
            _logger.LogInformation($"projecting DomainEvent of type '{domainEvent.EventType}' / '{id}'");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ProjectionStoreContext>();
                var database = scope.ServiceProvider.GetService<IConfigurationDatabase>();

                using (var transaction = context.Database.BeginTransaction())
                {
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

                        transaction.Commit();

                        _logger.LogInformation($"transaction '{transaction.TransactionId}' committed");

                        return true;
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogCritical($"could not project domain-event of type '{domainEvent.EventType}', " +
                                            $"rolling back transaction '{transaction.TransactionId}' :{e}");
                        return false;
                    }
                    finally
                    {
                        _logger.LogTrace("forcing GC.Collect...");

                        GC.Collect();
                    }
                }
            }
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