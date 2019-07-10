using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.Models
{
    public class ProjectionNodeStatus
    {
        public ProjectionStatus CurrentStatus { get; set; }

        public DomainEvent LastEvent { get; set; }

        public DateTime LastEventAt { get; set; }

        public string LastEventId { get; set; }

        public long LastEventNumber { get; set; }

        public EventProjectionResult LastEventResult { get; set; }

        public string LastEventType { get; set; }

        public string NodeId { get; set; }
    }
}