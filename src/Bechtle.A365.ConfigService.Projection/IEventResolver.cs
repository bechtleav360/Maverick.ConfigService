using Bechtle.A365.ConfigService.Dto.DomainEvents;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Projection
{
    public interface IEventResolver
    {
        DomainEvent ToDomainEvent(ResolvedEvent resolvedEvent);
    }
}