using System;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    public interface IOutput : ILogger
    {
        LogLevel LogLevel { get; set; }

        (bool In, bool Out, bool Err) GetRedirections();

        bool IsStdDInRedirected();

        bool IsStdErrRedirected();

        bool IsStdOutRedirected();

        void WriteLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteErrorLine(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteVerboseLine(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void Write(string str, int level = 0, ConsoleColor color = ConsoleColor.White);

        void WriteError(string str, int level = 0, ConsoleColor color = ConsoleColor.Red);

        void WriteErrorSeparator();

        void WriteSeparator();

        void WriteVerbose(string str, int level = 0, ConsoleColor color = ConsoleColor.White);
    }
}