using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     create new snapshots of the most up-to-date version of DomainObjects
    /// </summary>
    public interface ISnapshotCreator
    {
        /// <summary>
        ///     get snapshots of all DomainObjects
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<StreamedObjectSnapshot>> CreateAllSnapshots(CancellationToken cancellationToken);

        /// <summary>
        ///     create incremental snapshots for all given <paramref name="streamedObjects"/>
        /// </summary>
        /// <param name="streamedObjects"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<StreamedObjectSnapshot>> CreateSnapshots(IList<StreamedObject> streamedObjects, CancellationToken cancellationToken);
    }
}