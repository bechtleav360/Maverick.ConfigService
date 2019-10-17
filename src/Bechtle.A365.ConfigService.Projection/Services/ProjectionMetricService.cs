using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Models;
using Microsoft.Extensions.Logging;


namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc />
    public class ProjectionMetricService : IMetricService
    {
        private readonly ILogger _logger;
        private ProjectionEventStatus _currentEvent;
        private bool _eventStoreConnected;
        private string _nodeId;
        private ProjectionStatus _nodeStatus;
        private long _queueLength;

        /// <inheritdoc />
        public ProjectionMetricService(ILogger<ProjectionMetricService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public ProjectionMetricService ClearCurrentEvent()
        {
            _currentEvent = new ProjectionEventStatus();
            return this;
        }

        /// <inheritdoc />
        public ProjectionMetricService Finish()
        {
            StatusChanged?.Invoke(this, new EventArgs());
            return Log();
        }

        /// <inheritdoc />
        public ProjectionNodeStatus GetCurrentStatus() => new ProjectionNodeStatus
        {
            CurrentStatus = _nodeStatus,
            CurrentEvent = _currentEvent,
            EventStoreConnected = _eventStoreConnected,
            NodeId = _nodeId,
            QueueLength = _queueLength
        };

        /// <inheritdoc />
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
                Id = eventId
                //Event = domainEvent
            };
            return this;
        }

        /// <inheritdoc />
        public ProjectionMetricService SetEventStoreConnected(bool connected)
        {
            _eventStoreConnected = connected;
            return this;
        }

        /// <inheritdoc />
        public ProjectionMetricService SetNodeId(string nodeId)
        {
            _nodeId = nodeId;
            return this;
        }

        /// <inheritdoc />
        public ProjectionMetricService SetQueueLength(long queueLength)
        {
            _queueLength = queueLength;
            return this;
        }

        /// <inheritdoc />
        public ProjectionMetricService SetStatus(ProjectionStatus status)
        {
            _nodeStatus = status;
            return this;
        }

        public event EventHandler StatusChanged;

        private ProjectionMetricService Log()
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation($"current status is: \r\n{JsonConvert.SerializeObject(GetCurrentStatus())}");
            return this;
        }
    }
}