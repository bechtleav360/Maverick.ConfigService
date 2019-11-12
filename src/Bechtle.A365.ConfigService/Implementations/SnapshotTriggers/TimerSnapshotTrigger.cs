using System;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCrontab;
using Timer = System.Timers.Timer;

namespace Bechtle.A365.ConfigService.Implementations.SnapshotTriggers
{
    /// <summary>
    ///     schedule-based snapshot-trigger
    /// </summary>
    public class TimerSnapshotTrigger : ISnapshotTrigger
    {
        private readonly ILogger _logger;
        private readonly Timer _timer;

        private IConfiguration _configuration;

        /// <inheritdoc />
        public TimerSnapshotTrigger(ILogger<TimerSnapshotTrigger> logger)
        {
            _logger = logger;
            _timer = new Timer {AutoReset = false};
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }

        /// <inheritdoc />
        public void Configure(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public event EventHandler SnapshotTriggered;

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken)
        {
            await Task.Yield();

            var interval = _configuration.GetSection("Interval").Get<string>();

            if (string.IsNullOrWhiteSpace(interval))
            {
                _logger.LogWarning("no interval configured via 'SnapshotConfiguration:Triggers:{{NAME}}:Trigger:Interval'");
                return;
            }

            _logger.LogInformation($"using interval '{interval}'");

            // creating snapshots in sub-minute resolution is not necessary
            var schedule = CrontabSchedule.Parse(interval, new CrontabSchedule.ParseOptions {IncludingSeconds = false});

            // calculate next interval and
            // account for inaccuracy of Timer by adding another second on top
            var nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow);
            var sleepTime = nextOccurrence - DateTime.UtcNow + TimeSpan.FromSeconds(1);

            _logger.LogInformation($"using interval '{interval}' the next occurence / trigger is at '{nextOccurrence:O}' - sleeping for {sleepTime:c}");

            _timer.Elapsed += (sender, args) => SnapshotTriggered?.Invoke(this, EventArgs.Empty);
            _timer.Interval = sleepTime.TotalMilliseconds;
            _timer.Start();
        }
    }
}