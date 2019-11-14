using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces
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
        Task<IList<DomainObjectSnapshot>> CreateAllSnapshots(CancellationToken cancellationToken);
    }
}