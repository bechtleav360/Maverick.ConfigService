using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    public interface IOutput : ILogger
    {
        LogLevel LogLevel { get; set; }

        (bool In, bool Out, bool Err) Redirections { get; }

        bool IsInputRedirected { get; }

        bool IsErrorRedirected { get; }

        bool IsOutputRedirected { get; }

        void WriteLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteErrorLine(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteVerboseLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void Write(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteError(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteErrorSeparator();

        void WriteSeparator();

        void WriteVerbose(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteTable<T>(IEnumerable<T> items, Func<T, Dictionary<string, object>> propertySelector);
    }
}