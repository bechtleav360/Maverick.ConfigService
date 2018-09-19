using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    public interface IConfigStore
    {
        Task WriteEvent(DomainEvent domainEvent);

        DomainEvent[] GetAll();
    }
}