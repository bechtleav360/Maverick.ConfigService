using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Subcommand(typeof(ListMigrationsCommand))]
    [Command("migrate", Description = "Migrate the Target Database to the highest supported Version")]
    public class MigrateCommand : SubCommand<Program>
    {
        private readonly ProjectionStoreContext _context;

        /// <inheritdoc />
        public MigrateCommand(IConsole console, ProjectionStoreContext context)
            : base(console)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            try
            {
                await _context.Database.MigrateAsync();

                return 0;
            }
            catch (Exception e)
            {
                Logger.LogError($"couldn't execute migrations: {e}");
                return 1;
            }
        }
    }
}