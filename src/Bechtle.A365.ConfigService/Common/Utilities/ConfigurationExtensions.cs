using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Common.Utilities
{
    /// <summary>
    ///     Extensions for dealing with <see cref="IConfiguration"/>
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        ///     Use this Configuration and configure the global NLog-instance with it
        /// </summary>
        /// <param name="configuration">Configuration-Section that is being bound to NLog</param>
        /// <param name="logger">optional logger to write diagnostics to</param>
        /// <returns>given Configuration without changes</returns>
        public static IConfiguration ConfigureNLog(
            this IConfiguration configuration,
            ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Configuration has been reloaded - applying LoggingConfiguration");

                var nLogSection = configuration.GetSection("LoggingConfiguration")?.GetSection("NLog");

                if (nLogSection is null)
                {
                    logger?.LogInformation("Section JsonLoggingConfiguration:NLog not found; skipping reconfiguration");
                    return configuration;
                }

                LogManager.Configuration = new NLogLoggingConfiguration(nLogSection);

                logger?.LogInformation("new LoggingConfiguration has been applied");
            }
            catch (Exception e)
            {
                logger?.LogWarning($"new LoggingConfiguration could not be applied: {e}");
            }

            return configuration;
        }
    }
}
