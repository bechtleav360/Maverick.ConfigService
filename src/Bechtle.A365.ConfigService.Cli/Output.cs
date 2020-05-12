using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    public class Output : IOutput
    {
        private const string Indent = "| ";

        private readonly IConsole _console;

        private readonly object _consoleLock = new object();

        private readonly StringBuilder _indentBuilder = new StringBuilder();

        private readonly object _indentBuilderLock = new object();

        /// <inheritdoc cref="Output" />
        public Output(IConsole console, LogLevel logLevel)
        {
            _console = console;
            LogLevel = logLevel;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => new DisposableDummy();

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

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
        public bool IsErrorRedirected => _console.IsErrorRedirected;

        /// <inheritdoc />
        public bool IsInputRedirected => _console.IsInputRedirected;

        /// <inheritdoc />
        public bool IsOutputRedirected => _console.IsOutputRedirected;

        /// <inheritdoc />
        public LogLevel LogLevel { get; set; }

        /// <inheritdoc />
        public (bool In, bool Out, bool Err) Redirections => (IsInputRedirected, IsOutputRedirected, IsErrorRedirected);

        /// <inheritdoc />
        public void Write(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteInternal(_console.Out, str, level, color, LogLevel.Information);

        /// <inheritdoc />
        public void Write(Stream stream)
            => WriteInternal(_console.Out, stream, LogLevel.Information);

        /// <inheritdoc />
        public void WriteError(string str, int level = 0, ConsoleColor color = ConsoleColor.Red)
            => WriteInternal(_console.Error, str, level, color, LogLevel.Error);

        /// <inheritdoc />
        public void WriteError(Stream stream)
            => WriteInternal(_console.Error, stream, LogLevel.Error);

        /// <inheritdoc />
        public void WriteErrorLine(string str, int level = 0, ConsoleColor color = ConsoleColor.Red)
            => WriteError(str + Environment.NewLine, level, color);

        /// <inheritdoc />
        public void WriteErrorSeparator()
            => WriteSeparatorInternal(_console.Error, ConsoleColor.Red);

        /// <inheritdoc />
        public void WriteLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => Write(str + Environment.NewLine, level, color);

        /// <inheritdoc />
        public void WriteSeparator()
            => WriteSeparatorInternal(_console.Out, ConsoleColor.White);

        /// <inheritdoc />
        public void WriteTable<T>(IEnumerable<T> items, Func<T, Dictionary<string, object>> propertySelector, Dictionary<string, TextAlign> cellAlignments)
        {
            var columns = new List<TableColumn>();

            // create and fill columns with data from items
            foreach (var item in items)
            {
                var properties = propertySelector.Invoke(item);
                foreach (var prop in properties)
                    if (columns.All(c => c.Name != prop.Key))
                    {
                        var alignment = TextAlign.Left;
                        cellAlignments?.TryGetValue(prop.Key, out alignment);

                        columns.Add(new TableColumn
                        {
                            Name = prop.Key,
                            Values = new List<string>(),
                            Alignment = alignment
                        });
                    }

                foreach (var prop in properties)
                    columns.First(c => c.Name == prop.Key)
                           .Values
                           .Add(prop.Value?.ToString() ?? string.Empty);
            }

            // order the columns by name
            columns = columns.OrderBy(c => c.Name)
                             .ToList();

            // write headers
            foreach (var column in columns)
                WriteTableCell(column.Name, column.Width);

            WriteTableRowEnd();

            // write header-separator
            foreach (var column in columns)
                WriteTableCell(new string('-', column.Width), column.Width);

            WriteTableRowEnd();

            // write actual data
            var entries = columns.Max(c => c.Values.Count);
            for (var i = 0; i < entries; ++i)
            {
                foreach (var column in columns)
                    WriteTableCell(column.Values[i], column.Width, column.Alignment);
                WriteTableRowEnd();
            }
        }

        /// <inheritdoc />
        public void WriteTable<T>(IEnumerable<T> items, Func<T, Dictionary<string, object>> propertySelector)
            => WriteTable(items, propertySelector, null);

        /// <inheritdoc />
        public void WriteVerbose(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteInternal(_console.Out, str, level, color, LogLevel.Debug);

        /// <inheritdoc />
        public void WriteVerbose(Stream stream)
            => WriteInternal(_console.Out, stream, LogLevel.Debug);

        /// <inheritdoc />
        public void WriteVerboseLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White)
            => WriteVerbose(str + Environment.NewLine, level, color);

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

        private void WriteInternal(TextWriter writer, Stream stream, LogLevel logLevel)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (_consoleLock)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1, true);

                do
                {
                    writer.WriteLine(reader.ReadLine());
                } while (!reader.EndOfStream);
            }
        }

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

        private void WriteTableCell(string value, int columnWidth) => WriteTableCell(value, columnWidth, TextAlign.Center);

        private void WriteTableCell(string value, int columnWidth, TextAlign alignment)
        {
            Write("|");

            int leftPadding = 0;
            int rightPadding = 0;
            int totalPadding = 0;

            switch (alignment)
            {
                case TextAlign.Left:
                    totalPadding = Math.Max(columnWidth + 2, value.Length + 2) - value.Length;
                    leftPadding = 1;
                    rightPadding = totalPadding;
                    break;

                case TextAlign.Center:
                    totalPadding = Math.Max(columnWidth + 2, value.Length + 2) - value.Length;
                    leftPadding = totalPadding / 2;
                    rightPadding = leftPadding + totalPadding % 2;
                    break;

                case TextAlign.Right:
                    totalPadding = Math.Max(columnWidth + 2, value.Length + 2) - value.Length;
                    leftPadding = totalPadding;
                    rightPadding = 1;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }

            Write(new string(' ', leftPadding) + value + new string(' ', rightPadding));
        }

        private void WriteTableRowEnd() => WriteLine("|");

        private sealed class DisposableDummy : IDisposable
        {
            /// <inheritdoc />
            public void Dispose()
            {
                // nothing to dispose of
            }
        }

        /// <summary>
        ///     one column of data within a table
        /// </summary>
        private struct TableColumn
        {
            /// <summary>
            ///     column-name / header
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     list of values top-down
            /// </summary>
            public List<string> Values { get; set; }

            /// <summary>
            ///     cache for <see cref="Width" />
            /// </summary>
            private int? _width;

            /// <summary>
            ///     maximum width of this column
            /// </summary>
            public int Width => _width ??= Math.Max(Values?.Max(x => x.Length) ?? 0, Name.Length);

            /// <summary>
            ///     Text Alignment for this cell
            /// </summary>
            public TextAlign Alignment { get; set; }
        }
    }
}