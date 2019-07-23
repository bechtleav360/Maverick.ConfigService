using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class MemoryEventHistoryService : IEventHistoryService
    {
        private readonly ILogger _logger;
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _projectionStore;

        /// <inheritdoc />
        public MemoryEventHistoryService(ILogger<MemoryEventHistoryService> logger,
                                         IEventStore eventStore,
                                         IProjectionStore projectionStore)
        {
            _logger = logger;
            _eventStore = eventStore;
            _projectionStore = projectionStore;
        }

        /// <inheritdoc />
        public async Task<EventStatus> GetEventStatus(DomainEvent domainEvent)
        {
            var status = EventStatus.Unknown;

            if (await IsEventInEventStore(domainEvent))
                status = EventStatus.Recorded;

            if (await IsEventAlreadyProjected(domainEvent))
                status = EventStatus.Projected;

            if (await IsEventSuperseded(domainEvent))
                status = EventStatus.Superseded;

            return status;
        }

        private async Task<bool> IsEventAlreadyProjected(DomainEvent domainEvent)
        {
            // get the list of events that have already been projected to DB
            // filter out those that don't have the same Type, they can't be what we search
            var metadataResult = await _projectionStore.Metadata.GetProjectedEventMetadata(p => p.Type == domainEvent.EventType);
            if (metadataResult.IsError)
            {
                _logger.LogWarning($"could not retrieve metadata for event of type '{domainEvent.EventType}'");
                return false;
            }

            _logger.LogDebug($"retrieved '{metadataResult.Data.Count}' metadata-records for projected events");

            _logger.LogDebug("streaming events to retrieve data for DomainEvents");

            var entry = (Index: (long) 0, Event: (DomainEvent) null);

            // stream the actual events from EventStore to get the underlying DomainEvents from them
            await _eventStore.ReplayEventsAsStream(
                e => e.EventType == domainEvent.EventType,
                tuple =>
                {
                    var (recordedEvent, @event) = tuple;

                    if (@event.Equals(domainEvent))
                    {
                        entry = (recordedEvent.EventNumber, @event);
                        return false;
                    }

                    return true;
                }, 128);

            // check if event is null in the tuple, because tuple is struct and never null
            var result = !(entry.Event is null);

            _logger.LogInformation(result
                                       ? $"DomainEvent '{domainEvent.EventType}' has been projected at ({entry.Index})"
                                       : $"DomainEvent '{domainEvent.EventType}' with same data could not be found in ES-Stream");

            return result;
        }

        private async Task<bool> IsEventInEventStore(DomainEvent domainEvent)
        {
            var result = false;

            // stream all events in EventStore and compare their payload-DomainEvent with what we're given
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (_, @event) = tuple;

                if (@event.Equals(domainEvent))
                {
                    // set status and break further stream processing by returning false
                    result = true;
                    return false;
                }

                // continue stream-processing
                return true;
            }, 128);

            _logger.LogDebug(result
                                 ? $"DomainEvent '{domainEvent.EventType}' with same data has been found in ES-Stream"
                                 : $"DomainEvent '{domainEvent.EventType}' with same data could not be found in ES-Stream");

            return result;
        }

        private Task<bool> IsEventSuperseded(DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                // events that are unique once written
                case StructureCreated _:
                case EnvironmentCreated _:
                case DefaultEnvironmentCreated _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can't be superseded (fixed)");
                    return Task.FromResult(false);

                // @TODO: see if the modifications of domainEvent have been overwritten, which would cause it to be Superseded
                // events that appear routinely and
                // even though it's already in EventStore it can be written again
                case ConfigurationBuilt _:
                case EnvironmentKeysImported _:
                case EnvironmentKeysModified _:
                case StructureVariablesModified _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can be superseded");
                    return Task.FromResult(true);

                // not implemented
                case StructureDeleted _:
                case EnvironmentDeleted _:
                    _logger.LogInformation($"DomainEvent '{domainEvent.EventType}' with same data can be superseded (fixed)");
                    return Task.FromResult(true);
            }

            return Task.FromResult(true);
        }
    }
}