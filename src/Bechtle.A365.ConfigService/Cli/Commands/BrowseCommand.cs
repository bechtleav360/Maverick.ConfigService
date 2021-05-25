using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("browse", Description = "browse data contained in the ConfigService")]
    [Subcommand(
        typeof(BrowseConfigsCommand),
        typeof(BrowseEnvironmentsCommand),
        typeof(BrowseUnusedEnvironmentKeysCommand))]
    public class BrowseCommand : SubCommand<CliBase>
    {
        /// <inheritdoc />
        public BrowseCommand(IOutput output) : base(output)
        {
        }

        /// <inheritdoc />
        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}