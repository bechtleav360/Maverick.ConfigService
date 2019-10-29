using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedEvent
    {
        public DomainEvent DomainEvent { get; set; }

        public long Version { get; set; }

        public DateTime UtcTime { get; set; }
    }
}