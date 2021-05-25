using Bechtle.A365.ServiceBase.Commands;

namespace Bechtle.A365.ConfigService.Cli
{
    /// <summary>
    ///     Subcommand of a given Base-Command
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SubCommand<T> : CommandBase
    {
        /// <inheritdoc />
        public SubCommand(IOutput output)
            : base(output)
        {
        }

        /// <summary>
        ///     reference to Parent-Command
        /// </summary>
        protected T Parent { get; set; }
    }
}
