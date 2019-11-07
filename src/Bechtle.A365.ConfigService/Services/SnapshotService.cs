using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<ISnapshotTrigger> _completeTriggers;
        private readonly List<ISnapshotTrigger> _incrementalTriggers;

        private CancellationTokenSource _completeTriggerTokenSource;
        private CancellationTokenSource _incrementalTriggerTokenSource;

        /// <inheritdoc />
        public SnapshotService(IServiceProvider provider,
                               IConfiguration configuration,
                               ILogger<SnapshotService> logger)
            : base(provider)
        {
            _provider = provider;
            _configuration = configuration;
            _logger = logger;
            _completeTriggers = new List<ISnapshotTrigger>();
            _incrementalTriggers = new List<ISnapshotTrigger>();
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();
                var provider = scope.ServiceProvider;

                try
                {
                    _completeTriggerTokenSource = new CancellationTokenSource();
                    _incrementalTriggerTokenSource = new CancellationTokenSource();
                    var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                                                                                       _completeTriggerTokenSource.Token,
                                                                                       _incrementalTriggerTokenSource.Token);

                    await CreateTriggers(cancellationToken, provider);

                    try
                    {
                        // wait for OnSnapshotTriggered to fire and cancel _completeTriggerTokenSource
                        while (!linkedSource.IsCancellationRequested)
                            await Task.Delay(TimeSpan.FromSeconds(10), linkedSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // we expect TaskCanceled to be thrown once OnSnapshotTriggered is fired
                        // but we only care about the timer finishing
                    }

                    // if we pass the previous block something has happened, and we should clean up before continuing
                    _logger.LogDebug("disposing all SnapshotTriggers");
                    foreach (var trigger in _completeTriggers.Concat(_incrementalTriggers))
                        trigger.Dispose();

                    // if the overall CT was cancelled we stop, otherwise we continue and loop
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var kreator = provider.GetRequiredService<ISnapshotCreator>();

                    if (_incrementalTriggerTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug("incremental-snapshot has been triggered");

                        // @TODO: ask *someone* for all currently-available DomainObjects - may even be limited to previous snapshots
                        var existingDomainObjects = new List<StreamedObject>();

                        var snapshots = await kreator.CreateSnapshots(existingDomainObjects, cancellationToken);
                        await SaveSnapshots(provider, snapshots, cancellationToken);
                    }
                    else if (_completeTriggerTokenSource.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug("complete-snapshot has been triggered");

                        var snapshots = await kreator.CreateAllSnapshots(cancellationToken);
                        await SaveSnapshots(provider, snapshots, cancellationToken);
                    }
                    else
                        _logger.LogWarning("SnapshotTrigger-wait-loop has been cancelled " +
                                           "without triggering one of the three tokens - unknown behaviour");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "error while running SnapshotService in background");
                }
            }
        }

        private Task CreateTriggers(CancellationToken cancellationToken, IServiceProvider provider)
        {
            _completeTriggers.Clear();
            _incrementalTriggers.Clear();

            _logger.LogDebug($"retrieving all instances of {nameof(ISnapshotTrigger)}");

            var triggerConfigSection = _configuration.GetSection("SnapshotConfiguration:Triggers");
            var triggerConfig = triggerConfigSection.Get<Dictionary<string, TriggerConfiguration>>();

            _logger.LogDebug($"found ISnapshotTrigger-Configs for '{triggerConfig.Count}' triggers ({string.Join(", ", triggerConfig.Keys)})");

            // map the trigger-configs to actual new instances if possible
            var triggers = triggerConfig.Select(pair =>
            {
                var (name, config) = pair;
                ISnapshotTrigger triggerInstance = pair.Value.Type switch
                {
                    "Timer" => provider.GetRequiredService<TimerSnapshotTrigger>() as ISnapshotTrigger,
                    "EventLag" => provider.GetRequiredService<NumberThresholdSnapshotTrigger>() as ISnapshotTrigger,
                    _ => throw new ArgumentOutOfRangeException($"SnapshotConfiguration:Triggers:{name}:Type",
                                                               $"SnapshotConfiguration:Triggers:{name}:Type; '{config.Type}' is not supported")
                };

                // pass the instances "Trigger" section to the actual instance
                triggerInstance.Configure(triggerConfigSection.GetSection($"{name}:Trigger"));

                return (Name: name, Instance: triggerInstance);
            }).ToList();

            // get trigger=>snapshot associations, to map them later
            var completeAssociations = _configuration.GetSection("SnapshotConfiguration:Snapshots:Complete").Get<string[]>() ?? new string[0];
            var incrementalAssociations = _configuration.GetSection("SnapshotConfiguration:Snapshots:Incremental").Get<string[]>() ?? new string[0];

            // define as functions so i don't have to write so much duplicated code, or compress it until it's unreadable
            bool SearchTriggerAssociation(string association)
                => triggers.Any(tuple => tuple.Name.Equals(association, StringComparison.OrdinalIgnoreCase));

            ISnapshotTrigger GetTriggerInstance(string association)
                => triggers.First(tuple => tuple.Name.Equals(association, StringComparison.OrdinalIgnoreCase)).Instance;

            _completeTriggers.AddRange(completeAssociations.Where(SearchTriggerAssociation).Select(GetTriggerInstance));
            _incrementalTriggers.AddRange(incrementalAssociations.Where(SearchTriggerAssociation).Select(GetTriggerInstance));

            foreach (var trigger in _completeTriggers)
                trigger.SnapshotTriggered += OnCompleteSnapshotTriggered;

            foreach (var trigger in _incrementalTriggers)
                trigger.SnapshotTriggered += OnIncrementalSnapshotTriggered;

            return Task.WhenAll(_completeTriggers.Concat(_incrementalTriggers).Select(t => t.Start(cancellationToken)));
        }

        private void OnIncrementalSnapshotTriggered(object sender, EventArgs e)
        {
            _logger.LogCritical($"incremental snapshot has been triggered by {sender.GetType().Name}");

            // cancel the token, to signal ExecuteAsync to continue its work
            _incrementalTriggerTokenSource.Cancel();
        }

        private void OnCompleteSnapshotTriggered(object sender, EventArgs e)
        {
            _logger.LogCritical($"complete snapshot has been triggered by {sender.GetType().Name}");

            // cancel the token, to signal ExecuteAsync to continue its work
            _completeTriggerTokenSource.Cancel();
        }

        private class TriggerConfiguration
        {
            public string Type { get; set; }
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
    }
}