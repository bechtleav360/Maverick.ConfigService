using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Projection.SnapshotTriggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    public class SnapshotService : HostedService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly List<ISnapshotTrigger> _triggers;

        private CancellationToken _cancellationToken;

        public SnapshotService(IServiceProvider provider,
                               ILogger<SnapshotService> logger)
            : base(provider)
        {
            _provider = provider;
            _logger = logger;
            _triggers = new List<ISnapshotTrigger>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            await Task.Yield();
            await RegisterSnapshotTriggers();
        }

        private async Task RegisterSnapshotTriggers()
        {
            if (_cancellationToken.IsCancellationRequested)
                return;

            _logger.LogDebug($"retrieving all instances of {nameof(ISnapshotTrigger)}");

            _triggers.Clear();
            _triggers.AddRange(_provider.GetServices<ISnapshotTrigger>());

            foreach (var trigger in _triggers)
                trigger.SnapshotTriggered += OnSnapshotTriggered;

            if (_cancellationToken.IsCancellationRequested)
                return;

            foreach (var trigger in _triggers)
                await trigger.Start(_cancellationToken);
        }

        private void OnSnapshotTriggered(object sender, EventArgs e)
        {
            _logger.LogCritical($"SNAPSHOT HAS BEEN TRIGGERED BY {sender.GetType().Name}");

            _logger.LogDebug("disposing all SnapshotTriggers");
            foreach (var trigger in _triggers)
                trigger.Dispose();

            _logger.LogDebug("re-registering all SnapshotTriggers");
            RegisterSnapshotTriggers().RunSync();
        }
    }
}