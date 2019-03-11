using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    [HelpOption("--help")]
    public abstract class CommandBase
    {
        /// <inheritdoc />
        protected CommandBase(IConsole console)
        {
            Console = console;
            LoggerInstance = new ConsoleLogger(Console, LogLevel.Error);
            Logger = LoggerInstance;
        }

        [Option("-s|--service")]
        public string ConfigServiceEndpoint { get; set; }

        [Option("-v|--verbose", Description = "Increase verbosity of logging each time it's supplied")]
        public bool[] Verbose
        {
            set
            {
                if (value.Length == 1)
                    LoggerInstance.LogLevel = LogLevel.Information;
                else if (value.Length == 2)
                    LoggerInstance.LogLevel = LogLevel.Debug;
                else if (value.Length >= 3)
                    LoggerInstance.LogLevel = LogLevel.Trace;
                else
                    LoggerInstance.LogLevel = LogLevel.Error;
            }
        }

        /// <inheritdoc cref="IConsole" />
        protected IConsole Console { get; }

        protected ILogger Logger { get; }

        private ConsoleLogger LoggerInstance { get; }

        protected virtual bool CheckParameters()
        {
            if (string.IsNullOrWhiteSpace(ConfigServiceEndpoint))
            {
                Logger.LogError($"no {nameof(ConfigServiceEndpoint)} given -- see help for more information");
                return false;
            }

            return true;
        }

        // ReSharper disable once UnusedMember.Global
        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }
    }
}