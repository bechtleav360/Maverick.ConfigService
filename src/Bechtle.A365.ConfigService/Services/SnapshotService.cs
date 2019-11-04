using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services.SnapshotTriggers;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     Service responsible for managing all available instances of <see cref="ISnapshotTrigger"/>,
    ///     and calling <see cref="ISnapshotStore"/> to save the created snapshots when required
    /// </summary>
    public class SnapshotService : HostedService
    {
        private readonly IServiceProvider _provider;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly List<ISnapshotTrigger> _triggers;

        private CancellationTokenSource _triggerTokenSource;

        /// <inheritdoc />
        public SnapshotService(IServiceProvider provider,
                               IConfiguration configuration,
                               ILogger<SnapshotService> logger)
            : base(provider)
        {
            _provider = provider;
            _configuration = configuration;
            _logger = logger;
            _triggers = new List<ISnapshotTrigger>();
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();

                try
                {
                    _triggerTokenSource = new CancellationTokenSource();
                    var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _triggerTokenSource.Token);

                    _logger.LogDebug($"retrieving all instances of {nameof(ISnapshotTrigger)}");

                    _triggers.Clear();
                    _triggers.AddRange(scope.ServiceProvider.GetServices<ISnapshotTrigger>());

                    foreach (var trigger in _triggers)
                        trigger.SnapshotTriggered += OnSnapshotTriggered;

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    foreach (var trigger in _triggers)
                        await trigger.Start(cancellationToken);

                    try
                    {
                        // wait for OnSnapshotTriggered to fire and cancel _triggerTokenSource
                        while (!linkedSource.IsCancellationRequested)
                            await Task.Delay(TimeSpan.FromSeconds(10), linkedSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // we expect TaskCanceled to be thrown once OnSnapshotTriggered is fired
                        // but we only care about the timer finishing
                    }

                    // if the overall CT was cancelled we stop, otherwise we continue and loop
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    _logger.LogDebug("disposing all SnapshotTriggers");
                    foreach (var trigger in _triggers)
                        trigger.Dispose();

                    var snapshots = await CreateSnapshots(scope.ServiceProvider, cancellationToken);
                    await SaveSnapshots(scope.ServiceProvider, snapshots, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "error while running SnapshotService in background");
                }
            }
        }

        private async Task<IList<StreamedObjectSnapshot>> CreateSnapshots(IServiceProvider provider, CancellationToken cancellationToken)
        {
            var kreator = provider.GetRequiredService<ISnapshotCreator>();

            return await kreator.CreateAllSnapshots(cancellationToken);
        }

        private async Task SaveSnapshots(IServiceProvider provider, IList<StreamedObjectSnapshot> snapshots, CancellationToken cancellationToken)
        {
            var stores = new List<ISnapshotStore>();

            if (_configuration.GetSection("SnapshotConfiguration:Stores:Postgres:Enabled").Get<bool>())
                stores.Add(provider.GetRequiredService<PostgresSnapshotStore>());

            foreach (var store in stores)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await store.SaveSnapshots(snapshots);
            }
        }

        private void OnSnapshotTriggered(object sender, EventArgs e)
        {
            _logger.LogCritical($"SNAPSHOT HAS BEEN TRIGGERED BY {sender.GetType().Name}");

            // cancel the token, to signal ExecuteAsync to continue its work
            _triggerTokenSource.Cancel();
        }
    }
}