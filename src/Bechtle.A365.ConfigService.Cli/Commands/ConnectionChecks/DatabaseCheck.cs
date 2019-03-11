using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class DatabaseCheck : IConnectionCheck
    {
        /// <inheritdoc />
        public string Name => "SQL-DB Availability";

        /// <inheritdoc />
        public async Task<TestResult> Execute(FormattedOutput output, TestParameters parameters)
        {
            output.Line("Connecting to SQL-DB using Effective Configuration");

            var configuration = ConfigurationCheck.EffectiveConfiguration.Get<ConfigServiceConfiguration>();

            if (configuration?.ProjectionStorage is null)
            {
                output.Line("Effective Configuration (ProjectionStorage) is null - see previous checks", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Effective Configuration is null"
                };
            }

            output.Line($"Using ConnectionString '{configuration.ProjectionStorage.ConnectionString}'", 1);

            try
            {
                using (var context = new DatabaseCheckContext(configuration.ProjectionStorage.ConnectionString))
                {
                    var migrations = (await context.Database.GetAppliedMigrationsAsync())?.ToList() ?? new List<string>();

                    output.Line($"Found '{migrations.Count}' Migrations", 1);
                    foreach (var migration in migrations)
                        output.Line(migration, 2);

                    var tables = context.MetadataString
                                        .FromSql("SELECT TABLE_NAME as Result FROM INFORMATION_SCHEMA.TABLES ORDER BY Result")
                                        .ToList()
                                        .Select(x => x.Result)
                                        .ToDictionary(table => table,
                                                      table => context.MetadataInt
                                                                      .FromSql($"SELECT COUNT(*) as Result FROM {table}", table)
                                                                      .First()
                                                                      .Result);

                    output.Line(1);

                    var longestTableName = tables.Keys.Max(x => x.Length);

                    output.Line($"Found {tables.Count} tables in the Schema", 1);
                    foreach (var (table, entries) in tables)
                        output.Line($"{table.PadRight(longestTableName, ' ')}: {entries:###,###,###,###} {(entries == 1 ? "row" : "rows")}", 2);
                }
            }
            catch (Exception e)
            {
                output.Line($"Error while querying database: {e.GetType().Name}; {e.Message}", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Database Exception"
                };
            }

            return new TestResult
            {
                Result = true,
                Message = string.Empty
            };
        }
    }

    public class DatabaseCheckContext : DbContext
    {
        private readonly string _connectionString;

        public DbQuery<AdHocQuery<string>> MetadataString { get; set; }

        public DbQuery<AdHocQuery<int>> MetadataInt { get; set; }

        /// <inheritdoc />
        public DatabaseCheckContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    public class AdHocQuery<T>
    {
        public T Result { get; set; }
    }
}