using Bechtle.A365.ConfigService.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    [Command(UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.StopParsingAndCollect)]
    [Subcommand(
        typeof(BrowseCommand),
        typeof(CompareCommand),
        typeof(ExportCommand),
        typeof(ImportCommand),
        typeof(MigrateCommand))]
    public class CliBase
    {
    }
}