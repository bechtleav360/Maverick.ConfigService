using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Models;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public interface IMetricService
    {
        ProjectionMetricService ClearCurrentEvent();

        ProjectionMetricService Finish();

        ProjectionNodeStatus GetCurrentStatus();

        ProjectionMetricService SetCurrentEvent(DomainEvent domainEvent,
                                                EventProjectionResult result,
                                                DateTime time,
                                                long eventIndex,
                                                string eventId);

        ProjectionMetricService SetEventStoreConnected(bool connected);

        ProjectionMetricService SetNodeId(string nodeId);

        ProjectionMetricService SetQueueLength(long queueLength);

        ProjectionMetricService SetStatus(ProjectionStatus status);

        event EventHandler StatusChanged;
    }
}