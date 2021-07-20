using Bechtle.A365.ConfigService.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    /// <summary>
    ///     Entrypoint for all CLI-Commands in this Application
    /// </summary>
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