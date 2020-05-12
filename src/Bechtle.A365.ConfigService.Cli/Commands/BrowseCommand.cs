using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("browse", Description = "browse data contained in the ConfigService")]
    [Subcommand(
        typeof(BrowseConfigsCommand),
        typeof(BrowseEnvironmentsCommand),
        typeof(BrowseUnusedEnvironmentKeysCommand))]
    public class BrowseCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public BrowseCommand(IConsole console) : base(console)
        {
        }
    }
}