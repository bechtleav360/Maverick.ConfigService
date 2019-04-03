using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    [Command("list", Description = "List Migrations in the target DB and which Migrations this version can apply additionally")]
    public class ListMigrationsCommand : SubCommand<MigrateCommand>
    {
        private readonly IServiceProvider _provider;
        private string _connectionString;

        /// <inheritdoc />
        public ListMigrationsCommand(IConsole console, IServiceProvider provider)
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
                var migrations = await GetMigrations();

                if (!migrations.Any(m => string.IsNullOrWhiteSpace(m.Name)))
                {
                    var longestName = migrations.Max(m => m.Name.Length);

                    foreach (var migration in migrations)
                        Output.WriteLine($"{migration.Timestamp:yyyy/MM/dd HH:mm:ssK} | " +
                                         $"{migration.Name.PadRight(longestName)} | " +
                                         $"{migration.State} | " +
                                         $"{(migration.Supported ? "Supported" : "Unsupported")}");
                }
                else
                {
                    var longestName = migrations.Max(m => m.Name.Length);

                    foreach (var migration in migrations)
                        Output.WriteLine($"{migration.RawName.PadRight(longestName)} | " +
                                         $"{migration.State} | " +
                                         $"{(migration.Supported ? "Supported" : "Unsupported")}");
                }

                return 0;
            }
            catch (Exception e)
            {
                Output.WriteError($"couldn't query DB for migrations: {e}");
                return 1;
            }
        }

        private async Task<MigrationEntry[]> GetMigrations()
        {
            var context = _provider.GetService<ProjectionStoreContext>();

            var migrations = context.Database
                                    .GetMigrations()
                                    .ToDictionary(m => m, m => new MigrationEntry
                                    {
                                        RawName = m,
                                        State = MigrationState.Unknown,
                                        Supported = true
                                    });

            foreach (var appliedMigration in await context.Database.GetAppliedMigrationsAsync())
                if (migrations.TryGetValue(appliedMigration, out var migration))
                    migration.State = MigrationState.Applied;
                else
                    migrations.Add(appliedMigration, new MigrationEntry
                    {
                        RawName = appliedMigration,
                        State = MigrationState.Applied,
                        Supported = false
                    });

            foreach (var pendingMigration in await context.Database.GetPendingMigrationsAsync())
                if (migrations.TryGetValue(pendingMigration, out var migration))
                    migration.State = MigrationState.Pending;
                else
                    migrations.Add(pendingMigration, new MigrationEntry
                    {
                        RawName = pendingMigration,
                        State = MigrationState.Pending,
                        Supported = false
                    });

            var regex = new Regex(@"(?<timestamp>\d{14})_(?<name>[\w_\-\/]+)", RegexOptions.IgnoreCase);

            foreach (var migration in migrations.Values)
            {
                var match = regex.Match(migration.RawName);

                if (!match.Success)
                    continue;

                migration.Timestamp = DateTime.ParseExact(match.Groups["timestamp"].Value,
                                                          "yyyyMMddHHmmss",
                                                          CultureInfo.InvariantCulture);

                migration.Name = match.Groups["name"].Value;
            }

            return migrations.Select(kvp => kvp.Value)
                             .ToArray();
        }

        private class MigrationEntry
        {
            public string Name { get; set; }
            public string RawName { get; set; }

            public MigrationState State { get; set; }

            public bool Supported { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private enum MigrationState
        {
            Unknown = 0,
            Pending = 1 << 0,
            Applied = 1 << 1
        }
    }
}