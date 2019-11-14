using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     service to save incremental snapshots asynchronously to a <see cref="ISnapshotStore" />
    /// </summary>
    public class IncrementalSnapshotService : HostedServiceBase
    {
        private static readonly ConcurrentQueue<DomainObjectSnapshot> IncrementalSnapshotQueue = new ConcurrentQueue<DomainObjectSnapshot>();
        private readonly ILogger<IncrementalSnapshotService> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public IncrementalSnapshotService(IServiceProvider serviceProvider,
                                          ILogger<IncrementalSnapshotService> logger)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        ///     queue the given Snapshot for future storage
        /// </summary>
        /// <param name="snapshot"></param>
        public static void QueueSnapshot(DomainObjectSnapshot snapshot)
        {
            IncrementalSnapshotQueue.Enqueue(snapshot);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (!IncrementalSnapshotQueue.TryDequeue(out var snapshot) || snapshot is null)
                    continue;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<ISnapshotStore>();

                    await store.SaveSnapshots(new[] {snapshot});
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "unable to store incremental snapshot " +
                                          $"DataType='{snapshot.DataType}'; " +
                                          $"Identifier='{snapshot.Identifier}'; " +
                                          $"Version='{snapshot.Version}'; " +
                                          $"JsonData.Length={snapshot.JsonData?.Length ?? -1}");
                }
            }
        }
    }
}