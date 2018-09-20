using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    /// </summary>
    public interface IConfigStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task WriteEvent(DomainEvent domainEvent);
    }
}