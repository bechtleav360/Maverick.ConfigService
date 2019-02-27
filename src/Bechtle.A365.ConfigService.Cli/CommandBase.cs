using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    [HelpOption("--help")]
    public abstract class CommandBase
    {
        /// <inheritdoc cref="IConsole" />
        protected IConsole Console { get; }

        /// <inheritdoc />
        protected CommandBase(IConsole console)
        {
            Console = console;
        }

        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }

        protected virtual void LogError(string message) => Log(LogLevel.Error, message, ConsoleColor.Red);

        protected virtual void LogInfo(string message) => Log(LogLevel.Info, message, ConsoleColor.White);

        protected virtual void LogDebug(string message) => Log(LogLevel.Debug, message, ConsoleColor.Gray);

        private void Log(LogLevel level, string message, ConsoleColor foreground)
        {
            Console.ForegroundColor = foreground;

            var formattedMessage = $"{DateTime.Now:O} {level} {message}";

            switch (level)
            {
                case LogLevel.Error:
                    Console.Error.WriteLine(formattedMessage);
                    break;

                case LogLevel.Info:
                case LogLevel.Debug:
                    Console.WriteLine(formattedMessage);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
            
            Console.ResetColor();
        }

        private enum LogLevel
        {
            Error,
            Info,
            Debug
        }
    }
}