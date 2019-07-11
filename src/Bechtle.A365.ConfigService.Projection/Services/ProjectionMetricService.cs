using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class ProjectionMetricService
    {
        private readonly ILogger _logger;
        private ProjectionEventStatus _currentEvent;
        private ProjectionEventStatus _lastEvent;
        private string _nodeId;
        private ProjectionStatus _nodeStatus;

        public ProjectionMetricService(ILogger<ProjectionMetricService> logger)
        {
            _logger = logger;
        }

        public ProjectionMetricService ClearCurrentEvent()
        {
            _currentEvent = new ProjectionEventStatus();
            return this;
        }

        public ProjectionNodeStatus GetCurrentStatus() => new ProjectionNodeStatus
        {
            NodeId = _nodeId,
            CurrentStatus = _nodeStatus,
            CurrentEvent = _currentEvent,
            LastEvent = _lastEvent
        };

        public ProjectionMetricService Log()
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation($"current status is: \r\n{JsonConvert.SerializeObject(GetCurrentStatus())}");
            return this;
        }

        public ProjectionMetricService SetCurrentEvent(DomainEvent domainEvent,
                                                       EventProjectionResult result,
                                                       DateTime time,
                                                       long eventIndex,
                                                       string eventId)
        {
            _currentEvent = new ProjectionEventStatus
            {
                At = time,
                Result = result,
                Type = domainEvent?.EventType ?? "unknown",
                Index = eventIndex,
                Id = eventId,
                Event = domainEvent
            };
            return this;
        }

        public ProjectionMetricService SetLastEvent(DomainEvent domainEvent,
                                                    EventProjectionResult result,
                                                    DateTime time,
                                                    long eventIndex,
                                                    string eventId)
        {
            _lastEvent = new ProjectionEventStatus
            {
                At = time,
                Result = result,
                Type = domainEvent?.EventType ?? "unknown",
                Index = eventIndex,
                Id = eventId,
                Event = domainEvent
            };
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