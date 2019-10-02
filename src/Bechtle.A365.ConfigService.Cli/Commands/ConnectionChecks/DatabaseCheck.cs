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
        public async Task<TestResult> Execute(IOutput output, TestParameters parameters, ApplicationSettings settings)
        {
            output.WriteLine("Connecting to SQL-DB using Effective Configuration");

            var configuration = settings.EffectiveConfiguration.Get<ConfigServiceConfiguration>();

            if (configuration?.ProjectionStorage is null)
            {
                output.WriteLine("Effective Configuration (ProjectionStorage) is null - see previous checks", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Effective Configuration is null"
                };
            }

            output.WriteLine($"Using ConnectionString '{configuration.ProjectionStorage.ConnectionString}'", 1);

            try
            {
                using (var context = new DatabaseCheckContext(configuration.ProjectionStorage.ConnectionString))
                {
                    var migrations = (await context.Database.GetAppliedMigrationsAsync())?.ToList() ?? new List<string>();

                    output.WriteLine($"Found '{migrations.Count}' Migrations", 1);
                    foreach (var migration in migrations)
                        output.WriteLine(migration, 2);

                    var tables = context.MetadataString
                                        .FromSqlInterpolated($"SELECT TABLE_NAME as Result FROM INFORMATION_SCHEMA.TABLES ORDER BY Result")
                                        .ToList()
                                        .Select(x => x.Result)
                                        .ToDictionary(table => table,
                                                      table => context.MetadataInt
                                                                      .FromSqlInterpolated($"SELECT COUNT(*) as Result FROM {table}")
                                                                      .First()
                                                                      .Result);

                    output.WriteLine(string.Empty, 1);

                    var longestTableName = tables.Keys.Max(x => x.Length) + 1;

                    output.WriteLine($"Found {tables.Count} tables in the Schema", 1);

                    var longestDigit = tables.Max(t => CountDigits(t.Value));
                    longestDigit += longestDigit / 3;

                    foreach (var (table, entries) in tables)
                        output.WriteLine($"{table.PadRight(longestTableName, ' ')}: " +
                                         $"{entries.ToString("###,###,###,###").PadLeft(longestDigit, ' ')} " +
                                         $"{(entries == 1 ? "row" : "rows")}", 2);
                }
            }
            catch (Exception e)
            {
                output.WriteLine($"Error while querying database: {e.GetType().Name}; {e.Message}", 1);
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

        /// <inheritdoc />
        public string Name => "SQL-DB Availability";

        private static int CountDigits(int number)
        {
            if (number == 0)
                return 1;

            var n = 0;
            while (number > 0)
            {
                number /= 10;
                ++n;
            }

            return n;
        }
    }

    public class DatabaseCheckContext : DbContext
    {
        private readonly string _connectionString;

        /// <inheritdoc />
        public DatabaseCheckContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbQuery<AdHocQuery<int>> MetadataInt { get; set; }

        public DbQuery<AdHocQuery<string>> MetadataString { get; set; }

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