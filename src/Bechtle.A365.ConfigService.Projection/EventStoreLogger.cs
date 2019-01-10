using System;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
// again, fuck you EventStore-guys for recreating one of the most ubiquitous interfaces in ALL OF .Net Core
using ESLogger = EventStore.ClientAPI.ILogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Projection
{
    /// <summary>
    ///     EventStore.ILogger implementation to forward log-calls to an actual ILogger implementation
    /// </summary>
    public class EventStoreLogger : ESLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        ///     create new instance with the given ILogger
        /// </summary>
        /// <param name="logger"></param>
        public EventStoreLogger(ILogger<IEventStoreConnection> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void Debug(string format, params object[] args) => _logger.LogDebug(string.Format(format, args));

        /// <inheritdoc />
        public void Debug(Exception ex, string format, params object[] args) => _logger.LogDebug(ex, string.Format(format, args));

        /// <inheritdoc />
        public void Error(string format, params object[] args) => _logger.LogError(string.Format(format, args));

        /// <inheritdoc />
        public void Error(Exception ex, string format, params object[] args) => _logger.LogError(ex, string.Format(format, args));

        /// <inheritdoc />
        public void Info(string format, params object[] args) => _logger.LogInformation(string.Format(format, args));

        /// <inheritdoc />
        public void Info(Exception ex, string format, params object[] args) => _logger.LogInformation(ex, string.Format(format, args));
    }
}