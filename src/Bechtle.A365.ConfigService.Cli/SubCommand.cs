using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    public class SubCommand<T> : CommandBase
    {
        /// <inheritdoc />
        public SubCommand(IConsole console) 
            : base(console)
        {
        }

        /// <summary>
        ///     reference to Parent-Command
        /// </summary>
        protected T Parent { get; set; }
    }
}