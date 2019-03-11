using Bechtle.A365.ConfigService.Cli.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    [Subcommand(
        typeof(ExportCommand),
        typeof(ImportCommand),
        typeof(TestCommand))]
    public class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        // ReSharper disable once UnusedMember.Local
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }
}