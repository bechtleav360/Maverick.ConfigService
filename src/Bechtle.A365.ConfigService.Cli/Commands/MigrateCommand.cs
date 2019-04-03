using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Subcommand(typeof(ListMigrationsCommand))]
    [Command("migrate", Description = "Migrate the Target Database to the highest supported Version")]
    public class MigrateCommand : SubCommand<Program>
    {
        private readonly IServiceProvider _provider;
        private string _connectionString;

        /// <inheritdoc />
        public MigrateCommand(IConsole console, IServiceProvider provider)
            : base(console)
        {
            _provider = provider;
        }

        [Option("-c|--connection-string", CommandOptionType.SingleValue, Description = "ConnectionString to use for Connecting to the Database")]
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                _provider.GetService<ApplicationSettings>().ConnectionString = value;
            }
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            try
            {
                var context = _provider.GetService<ProjectionStoreContext>();
                await context.Database.MigrateAsync();

                return 0;
            }
            catch (Exception e)
            {
                Output.WriteError($"couldn't execute migrations: {e}");
                return 1;
            }
        }
    }
}