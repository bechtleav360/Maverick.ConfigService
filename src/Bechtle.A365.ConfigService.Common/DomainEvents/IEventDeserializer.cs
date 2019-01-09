using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public interface IEventDeserializer
    {
        DomainEvent ToDomainEvent(ResolvedEvent resolvedEvent);
    }
}