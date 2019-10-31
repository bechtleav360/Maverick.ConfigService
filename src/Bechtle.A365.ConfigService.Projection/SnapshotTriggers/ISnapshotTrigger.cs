using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Projection.SnapshotTriggers
{
    public interface ISnapshotTrigger : IDisposable
    {
        event EventHandler SnapshotTriggered;

        Task Start(CancellationToken cancellationToken);
    }
}