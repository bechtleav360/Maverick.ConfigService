using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public interface IEventDeserializer
    {
        bool ToDomainEvent(ResolvedEvent resolvedEvent, out DomainEvent domainEvent);
    }
}