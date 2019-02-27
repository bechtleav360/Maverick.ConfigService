using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Bechtle.A365.ConfigService.Cli
{
    [Subcommand(
        typeof(ExportCommand),
        typeof(ImportCommand))]
    public class Program : CommandBase
    {
        /// <inheritdoc />
        public Program(IConsole console) : base(console)
        {
        }

        [Required]
        [Option("-s|--service")]
        public string ConfigServiceEndpoint { get; set; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
    }
}