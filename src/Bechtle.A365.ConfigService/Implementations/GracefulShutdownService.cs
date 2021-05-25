using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     handle all required tasks to let this service shutdown gracefully
    /// </summary>
    public class GracefulShutdownService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        /// <inheritdoc cref="GracefulShutdownService" />
        public GracefulShutdownService(
            IHostApplicationLifetime lifetime,
            ILogger<GracefulShutdownService> logger)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("registering OnApplicationStopping handler");

            _lifetime.ApplicationStopping.Register(LogManager.Shutdown);

            return Task.CompletedTask;
        }
    }
}
