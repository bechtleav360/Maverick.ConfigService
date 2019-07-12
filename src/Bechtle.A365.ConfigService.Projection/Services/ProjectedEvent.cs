using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class ProjectedEvent
    {
        public DomainEvent DomainEvent { get; set; }

        public string Id { get; set; }

        public long Index { get; set; }
    }
}