using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Services.SnapshotTriggers
{
    /// <summary>
    ///     component that can trigger the creation of new snapshots for all available <see cref="StreamedObject"/>
    /// </summary>
    public interface ISnapshotTrigger : IDisposable
    {
        /// <summary>
        ///     event that is triggered once the component decides it's time for new snapshots
        /// </summary>
        event EventHandler SnapshotTriggered;

        /// <summary>
        ///     pass the instance-configuration to the actual instance
        /// </summary>
        /// <param name="configuration"></param>
        void Configure(IConfiguration configuration);

        /// <summary>
        ///     entry-point for all Triggers, called some time after creation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Start(CancellationToken cancellationToken);
    }
}