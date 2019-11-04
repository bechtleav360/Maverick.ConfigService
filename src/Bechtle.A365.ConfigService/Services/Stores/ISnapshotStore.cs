using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    // @TODO: each new instance of ISnapshotStore has to be explicitly used by SnapshotService
    /// <summary>
    ///     Store, to retrieve Snapshots of <see cref="StreamedObject"/>
    /// </summary>
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

        /// <summary>
        ///     get the latest snapshot - if possible - of the <see cref="StreamedConfigurationList"/>
        /// </summary>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetConfigurationList();

        /// <summary>
        ///     get the latest snapshot - if possible - of the given <see cref="StreamedConfiguration"/>
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetConfiguration(ConfigurationIdentifier identifier);

        /// <summary>
        ///     save the given <see cref="StreamedObjectSnapshot"/> to the configured Store
        /// </summary>
        /// <param name="snapshots"></param>
        /// <returns></returns>
        Task<IResult> SaveSnapshots(IList<StreamedObjectSnapshot> snapshots);
    }
}