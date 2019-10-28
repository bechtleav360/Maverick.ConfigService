using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    public interface ISnapshotStore
    {
        /// <summary>
        ///     get the latest snapshot - if possible - from the EnvironmentList
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetEnvironmentList();

        /// <summary>
        ///     get the latest snapshot - if possible - from the given Environment
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetEnvironment(EnvironmentIdentifier identifier);
    }
}