using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     component that can trigger the creation of new snapshots for all available <see cref="DomainObjects.DomainObject" />
    /// </summary>
    public interface ISnapshotTrigger : IDisposable
    {
        /// <summary>
        ///     pass the instance-configuration to the actual instance
        /// </summary>
        /// <param name="configuration"></param>
        void Configure(IConfiguration configuration);

        /// <summary>
        ///     event that is triggered once the component decides it's time for new snapshots
        /// </summary>
        event EventHandler<EventArgs> SnapshotTriggered;

        /// <summary>
        ///     entry-point for all Triggers, called some time after creation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Start(CancellationToken cancellationToken);
    }
}