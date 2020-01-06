using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli
{
    [HelpOption("--help")]
    public abstract class CommandBase
    {
        /// <inheritdoc cref="CommandBase" />
        protected CommandBase(IConsole console)
        {
            Output = new Output(console, LogLevel.Information);
        }

        [Option("-s|--service")]
        public string ConfigServiceEndpoint { get; set; }

        [Option("-v|--verbose", Description = "Increase verbosity of logging each time it's supplied")]
        [SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "used to set LogLevel via Arguments")]
        public bool[] Verbose
        {
            set
            {
                if (value.Length <= 1)
                    Output.LogLevel = LogLevel.Information;
                else if (value.Length == 2)
                    Output.LogLevel = LogLevel.Debug;
                else if (value.Length >= 3)
                    Output.LogLevel = LogLevel.Trace;
            }
        }

        [Option("-q|--quiet", Description = "Decrease verbosity of logging each time it's supplied")]
        [SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "used to set LogLevel via Arguments")]
        public bool[] Quiet
        {
            set
            {
                if (value.Length >= 2)
                    Output.LogLevel = LogLevel.None;
                else if (value.Length == 1)
                    Output.LogLevel = LogLevel.Error;
            }
        }

        protected IOutput Output { get; set; }

        protected virtual bool CheckParameters()
        {
            if (string.IsNullOrWhiteSpace(ConfigServiceEndpoint))
            {
                Output.WriteError($"no {nameof(ConfigServiceEndpoint)} given -- see help for more information");
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