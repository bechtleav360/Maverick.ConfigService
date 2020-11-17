using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("migrate", Description = "browse data contained in the ConfigService")]
    [Subcommand(typeof(MigrateStreamCommand))]
    public class MigrateCommand : SubCommand<Program>
    {
        /// <inheritdoc />
        public MigrateCommand(IConsole console) : base(console)
        {
        }
    }
}