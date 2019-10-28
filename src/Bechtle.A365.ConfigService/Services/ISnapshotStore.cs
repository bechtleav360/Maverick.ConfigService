using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    public interface ISnapshotStore
    {
        /// <summary>
        ///     get the latest snapshot - if possible - of the <see cref="StreamedEnvironmentList"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetEnvironmentList();

        /// <summary>
        ///     get the latest snapshot - if possible - of the given <see cref="StreamedEnvironment"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetEnvironment(EnvironmentIdentifier identifier);

        /// <summary>
        ///     get the latest snapshot - if possible - of the <see cref="StreamedStructureList"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetStructureList();

        /// <summary>
        ///     get the latest snapshot - if possible - of the given <see cref="StreamedStructure"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetStructure(StructureIdentifier identifier);
    }
}