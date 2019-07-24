﻿using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class MemoryEventHistoryService : IEventHistoryService
    {
        private readonly IEventStore _eventStore;
        private readonly ILogger _logger;
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
            var status = await GetProjectedStatus(domainEvent);

            if (status == EventStatus.Unknown)
                return status;

            if (await IsEventSuperseded(domainEvent))
                return EventStatus.Superseded;

            return status;
        }

        private async Task<EventStatus> GetProjectedStatus(DomainEvent domainEvent)
        {
            // get the list of events that have already been projected to DB
            // filter out those that don't have the same Type, they can't be what we search
            var metadataResult = await _projectionStore.Metadata.GetProjectedEventMetadata(p => p.Type == domainEvent.EventType);
            if (metadataResult.IsError)
            {
                _logger.LogWarning($"could not retrieve metadata for event of type '{domainEvent.EventType}'");
                return EventStatus.Unknown;
            }

            var projectedEventIds = metadataResult.Data
                                                  .Select(e => e.Index)
                                                  .ToList();

            _logger.LogDebug($"retrieved '{metadataResult.Data.Count}' metadata-records for projected events");

            var result = EventStatus.Unknown;

            await _eventStore.ReplayEventsAsStream(
                e => e.EventType == domainEvent.EventType,
                tuple =>
                {
                    var (recordedEvent, @event) = tuple;

                    if (@event.Equals(domainEvent))
                    {
                        // set status and continue processing
                        // status could get more specific than Recorded
                        _logger.LogDebug($"DomainEvent '{domainEvent.EventType}' with same data has been found in ES-Stream");
                        result = EventStatus.Recorded;
                    }

                    // if projectedEventIds contains the streamed ID, we can assume something
                    // like the given event has already been projected
                    if (projectedEventIds.Contains(recordedEvent.EventNumber))
                    {
                        // set status and break further stream processing by returning false
                        // status can't become more specific than Projected
                        _logger.LogDebug($"DomainEvent '{domainEvent.EventType}' has been projected at '{recordedEvent.EventNumber}'");
                        result = EventStatus.Projected;
                        return false;
                    }

                    // continue stream-processing
                    return true;
                }, 128);

            _logger.LogInformation($"Status for DomainEvent '{domainEvent.EventType}': {result:G} / {result:D}");

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

                // @TODO: find out if any of these events (EnvKeysImported, EnvKeysModified, StructVarModified) modified the used data in this Event
                //        additional challenge, THIS DomainEvent can happen multiple times and only the last one counts
                case ConfigurationBuilt _:

                // events that appear routinely and
                // even though it's already in EventStore it can be written again
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