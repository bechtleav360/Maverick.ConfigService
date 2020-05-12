using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    public interface IOutput : ILogger
    {
        bool IsErrorRedirected { get; }

        bool IsInputRedirected { get; }

        bool IsOutputRedirected { get; }

        LogLevel LogLevel { get; set; }

        (bool In, bool Out, bool Err) Redirections { get; }

        void Write(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void Write(Stream stream);

        void WriteError(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteError(Stream stream);

        void WriteErrorLine(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteErrorSeparator();

        void WriteLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteSeparator();

        void WriteTable<T>(IEnumerable<T> items, Func<T, Dictionary<string, object>> propertySelector);

        void WriteTable<T>(IEnumerable<T> items, Func<T, Dictionary<string, object>> propertySelector, Dictionary<string, TextAlign> cellAlignments);

        void WriteVerbose(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteVerbose(Stream stream);

        void WriteVerboseLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);
    }
}