using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     component that stores and retrieves current <see cref="EventStatus"/> for any given <see cref="DomainEvent"/>
    /// </summary>
    public interface IEventHistoryService
    {
        /// <summary>
        ///     get the status for a given DomainEvent.
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task<EventStatus> GetEventStatus(DomainEvent domainEvent);
    }
}