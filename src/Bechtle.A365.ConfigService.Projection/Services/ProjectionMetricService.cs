using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Models;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class ProjectionMetricService
    {
        private DomainEvent _lastEvent;
        private string _lastEventId;
        private long _lastEventNumber;
        private EventProjectionResult _lastEventResult;
        private DateTime _lastEventTime;

        private string _nodeId;
        private ProjectionStatus _nodeStatus;

        public ProjectionNodeStatus GetCurrentStatus() => new ProjectionNodeStatus
        {
            NodeId = _nodeId,
            CurrentStatus = _nodeStatus,
            LastEventAt = _lastEventTime,
            LastEventResult = _lastEventResult,
            LastEventType = _lastEvent?.EventType ?? "unknown",
            LastEventNumber = _lastEventNumber,
            LastEventId = _lastEventId,
            LastEvent = _lastEvent
        };

        public ProjectionMetricService SetLastEvent(DomainEvent domainEvent,
                                                    EventProjectionResult result,
                                                    DateTime time,
                                                    long eventNumber,
                                                    string eventId)
        {
            _lastEvent = domainEvent;
            _lastEventResult = result;
            _lastEventTime = time;
            _lastEventNumber = eventNumber;
            _lastEventId = eventId;
            return this;
        }

        public ProjectionMetricService SetNodeId(string nodeId)
        {
            _nodeId = nodeId;
            return this;
        }

        public ProjectionMetricService SetStatus(ProjectionStatus status)
        {
            _nodeStatus = status;
            return this;
        }
    }
}