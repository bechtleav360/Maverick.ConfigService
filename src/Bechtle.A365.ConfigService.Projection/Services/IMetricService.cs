using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Models;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public interface IMetricService
    {
        event EventHandler StatusChanged;

        ProjectionMetricService ClearCurrentEvent();

        ProjectionNodeStatus GetCurrentStatus();

        ProjectionMetricService Finish();

        ProjectionMetricService SetCurrentEvent(DomainEvent domainEvent,
                                                EventProjectionResult result,
                                                DateTime time,
                                                long eventIndex,
                                                string eventId);

        ProjectionMetricService SetEventStoreConnected(bool connected);

        ProjectionMetricService SetLastEvent(DomainEvent domainEvent,
                                             EventProjectionResult result,
                                             DateTime time,
                                             long eventIndex,
                                             string eventId);

        ProjectionMetricService SetNodeId(string nodeId);

        ProjectionMetricService SetQueueLength(long queueLength);

        ProjectionMetricService SetStatus(ProjectionStatus status);
    }
}