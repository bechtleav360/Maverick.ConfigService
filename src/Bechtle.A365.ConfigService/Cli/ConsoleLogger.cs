using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    /// <summary>
    ///     Implementation of <see cref="ILogger{TCategoryName}"/> that writes to <see cref="Console"/>
    /// </summary>
    public class ConsoleLogger : ILogger<ConsoleLogger>
    {
        private readonly IConsole _console;

        /// <summary>
        ///     LogLevel used to decide which messages to write or withhold
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <inheritdoc cref="ConsoleLogger" />
        public ConsoleLogger(IConsole console, LogLevel logLevel = LogLevel.None)
        {
            LogLevel = logLevel;
            _console = console;
        }

        // this implementation comes from ILogger<T>, but it's not clear if any parameters are nullable or not
        // this code assumed 'formatter' could be null before moving to a nullable-enabled project, so we're leaving it as is
        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string>? formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                case LogLevel.Warning:
                    _console.WriteLine(formatter?.Invoke(state, exception) ?? string.Empty);
                    break;

                case LogLevel.Error:
                case LogLevel.Critical:
                    _console.Error.WriteLine(formatter?.Invoke(state, exception) ?? string.Empty);
                    break;

                case LogLevel.None:
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => new ConsoleScope();

        /// <summary>
        ///     dummy object to return *something* when BeginScope is called
        /// </summary>
        private sealed class ConsoleScope : IDisposable
        {
            /// <inheritdoc />
            public void Dispose()
            {
                // nothing to dispose of
            }
        }
    }
}
