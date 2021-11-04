using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Events;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     service to clean-up keys left over in the Journal after they expire on their own
    /// </summary>
    public class TemporaryKeyCleanupService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<TemporaryKeyCleanupService> _logger;

        /// <inheritdoc />
        public TemporaryKeyCleanupService(
            IServiceProvider provider,
            ILogger<TemporaryKeyCleanupService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // yielding back early to let whatever handles this continue before executing our low-priority cleanup
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    _logger.LogDebug("cleaning up empty keys from journal");
                    await ExecuteCleanup(cancellationToken);
                    _logger.LogDebug("cleanup finished");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "could not do temporary-key-cleanup");
                    _logger.LogInformation("cleanup failed, see previous errors");
                }
            }
        }

        private async Task ExecuteCleanup(CancellationToken cancellationToken)
        {
            _logger.LogDebug("creating DI-Scope for this run");

            using var scope = _provider.CreateScope();

            _logger.LogDebug("requesting services");

            if (cancellationToken.IsCancellationRequested)
                return;

            var tempStore = scope.ServiceProvider.GetRequiredService<ITemporaryKeyStore>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            IResult<IDictionary<string, IDictionary<string, string?>>> result = await tempStore.GetAll();

            if (cancellationToken.IsCancellationRequested)
                return;

            if (result.IsError)
            {
                _logger.LogWarning("could not retrieve keys from '{KeyStore}', will probably try again soon", nameof(ITemporaryKeyStore));
                return;
            }

            foreach (var (region, data) in result.CheckedData)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogDebug("searching through region '{TempKeyRegion}' for keys to clean up", region);
                var keysToExpire = new List<string>();

                int lastDotIndex = region.LastIndexOf('.');
                string structure = region[..lastDotIndex];
                int version = int.Parse(region[(lastDotIndex + 1)..]);

                _logger.LogDebug(
                    "extracted Structure: '{Structure}'; Version: '{Version}' from region '{TempKeyRegion}'",
                    structure,
                    version,
                    region);

                _logger.LogTrace("searching for expired keys");
                foreach ((string key, string? value) in data)
                    if (string.IsNullOrWhiteSpace(value))
                        keysToExpire.Add(key);

                _logger.LogDebug(
                    "found '{ExpiredKeyCount}' keys in '{TempKeyRegion}' that will be removed",
                    keysToExpire.Count,
                    region);

                if (keysToExpire.Any())
                    _logger.LogTrace(
                        "expired keys that will be removed from region '{TempKeyRegion}': {ExpiredKeys}",
                        region,
                        string.Join("; ", keysToExpire));

                if (cancellationToken.IsCancellationRequested)
                    return;

                _logger.LogTrace("removing keys from store");
                await tempStore.Remove(region, keysToExpire);

                _logger.LogTrace("publishing event for expired keys");
                await eventBus.Publish(
                    new EventMessage
                    {
                        Event = new TemporaryKeysExpired(structure, version, keysToExpire),
                        EventType = nameof(TemporaryKeysExpired)
                    });
            }
        }
    }
}
