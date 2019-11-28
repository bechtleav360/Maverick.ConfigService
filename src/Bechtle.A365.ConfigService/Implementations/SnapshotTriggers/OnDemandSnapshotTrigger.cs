using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Implementations.SnapshotTriggers
{
    /// <summary>
    ///     User-Facing trigger to be triggered when a new Snapshot is desired
    /// </summary>
    public class OnDemandSnapshotTrigger : ISnapshotTrigger
    {
        /// <summary>
        ///     User-Facing event to be triggered when a new Snapshot is desired
        /// </summary>
        public static event EventHandler<EventArgs> OnDemandSnapshotTriggered;

        /// <summary>
        ///     trigger a new On-Demand snapshot
        /// </summary>
        public static void TriggerOnDemandSnapshot()
        {
            OnDemandSnapshotTriggered?.Invoke(null, EventArgs.Empty);
        }

        /// <inheritdoc />
        public OnDemandSnapshotTrigger()
        {
            OnDemandSnapshotTriggered += OnOnDemandSnapshotTriggered;
        }

        private void OnOnDemandSnapshotTriggered(object sender, EventArgs e) => SnapshotTriggered?.Invoke(this, EventArgs.Empty);

        /// <inheritdoc />
        public void Dispose()
        {
            OnDemandSnapshotTriggered -= OnOnDemandSnapshotTriggered;
        }

        /// <inheritdoc />
        public void Configure(IConfiguration configuration)
        {
            // intentionally left empty
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs> SnapshotTriggered;

        /// <inheritdoc />
        public Task Start(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}