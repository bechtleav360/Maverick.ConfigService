using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public interface IDomainEventHandler<in T> where T : DomainEvent
    {
        Task HandleDomainEvent(T domainEvent);
    }
}