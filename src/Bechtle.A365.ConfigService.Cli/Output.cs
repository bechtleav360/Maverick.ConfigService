using System;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    public class Output : IOutput
    {
        public LogLevel LogLevel { get; set; }

        private const string Indent = "| ";

        private readonly IConsole _console;

        private readonly object _consoleLock = new object();

        private readonly StringBuilder _indentBuilder = new StringBuilder();

        private readonly object _indentBuilderLock = new object();

        /// <inheritdoc />
        public Output(IConsole console, LogLevel logLevel)
        {
            _console = console;
            LogLevel = logLevel;
        }

        /// <inheritdoc />
        public (bool In, bool Out, bool Err) GetRedirections() => (_console.IsInputRedirected,
                                                                      _console.IsOutputRedirected,
                                                                      _console.IsErrorRedirected);

        /// <inheritdoc />
        public bool IsStdDInRedirected() => _console.IsInputRedirected;

        /// <inheritdoc />
        public bool IsStdErrRedirected() => _console.IsErrorRedirected;

        /// <inheritdoc />
        public bool IsStdOutRedirected() => _console.IsOutputRedirected;

        /// <inheritdoc />
        public void WriteLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => Write(str + Environment.NewLine, level, color);

        /// <inheritdoc />
        public void WriteErrorLine(string str, int level = 0, ConsoleColor color = ConsoleColor.Red)
            => WriteError(str + Environment.NewLine, level, color);

        /// <inheritdoc />
        public void WriteVerboseLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteVerbose(str + Environment.NewLine, level, color);

        /// <inheritdoc />
        public void Write(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteInternal(_console.Out, str, level, color, LogLevel.Information);

        /// <inheritdoc />
        public void WriteError(string str, int level = 0, ConsoleColor color = ConsoleColor.Red)
            => WriteInternal(_console.Error, str, level, color, LogLevel.Error);

        /// <inheritdoc />
        public void WriteErrorSeparator()
            => WriteSeparatorInternal(_console.Error, ConsoleColor.Red);

        /// <inheritdoc />
        public void WriteSeparator()
            => WriteSeparatorInternal(_console.Out, ConsoleColor.White);

        /// <inheritdoc />
        public void WriteVerbose(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteInternal(_console.Out, str, level, color, LogLevel.Debug);

        private string GetIndent(int level)
        {
            lock (_indentBuilderLock)
            {
                // to be extra safe there is nothing in there...
                _indentBuilder.Clear();

                for (var i = 0; i < level; ++i)
                    _indentBuilder.Append(Indent);

                var result = _indentBuilder.ToString();

                _indentBuilder.Clear();

                return result;
            }
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    WriteVerboseLine(formatter(state, exception));
                    break;

                case LogLevel.Information:
                case LogLevel.Warning:
                    WriteLine(formatter(state, exception));
                    break;

                case LogLevel.Error:
                case LogLevel.Critical:
                    WriteErrorLine(formatter(state, exception));
                    break;

                case LogLevel.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => new DisposableDummy();

        private void WriteInternal(TextWriter writer, string str, int indentLevel, ConsoleColor color, LogLevel logLevel)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (_consoleLock)
            {
                _console.ForegroundColor = color;
                writer.Write(GetIndent(indentLevel) + str);
                _console.ResetColor();
            }
        }

        private void WriteSeparatorInternal(TextWriter writer, ConsoleColor color)
            => WriteInternal(writer, "\r\n------------------------------------\r\n\r\n", 0, color, 0);

        private class DisposableDummy : IDisposable
        {
            /// <inheritdoc />
            public void Dispose()
            {
            }
        }
    }
}