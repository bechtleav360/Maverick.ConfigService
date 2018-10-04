using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Projection
{
    public interface IEventDeserializer
    {
        DomainEvent ToDomainEvent(ResolvedEvent resolvedEvent);
    }
}