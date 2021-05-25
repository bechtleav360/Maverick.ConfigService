using System.Threading.Tasks;
using Bechtle.A365.ServiceBase.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("migrate", Description = "browse data contained in the ConfigService")]
    [Subcommand(typeof(MigrateStreamCommand))]
    public class MigrateCommand : SubCommand<CliBase>
    {
        /// <inheritdoc />
        public MigrateCommand(IOutput output) : base(output)
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
