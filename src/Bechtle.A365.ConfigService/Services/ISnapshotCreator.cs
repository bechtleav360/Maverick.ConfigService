using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     create new snapshots of the most up-to-date version of DomainObjects
    /// </summary>
    public interface ISnapshotCreator
    {
        /// <summary>
        ///     get snapshots of all DomainObjects found in the current <see cref="IEventStore"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<StreamedObjectSnapshot>> CreateAllSnapshots(CancellationToken cancellationToken);
    }
}