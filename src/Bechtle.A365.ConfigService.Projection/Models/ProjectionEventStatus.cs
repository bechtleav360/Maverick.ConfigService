using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.Models
{
    public class ProjectionEventStatus
    {
        public DomainEvent Event { get; set; }

        public DateTime At { get; set; }

        public string Id { get; set; }

        public long Index { get; set; }

        public EventProjectionResult Result { get; set; }

        public string Type { get; set; }
    }
}