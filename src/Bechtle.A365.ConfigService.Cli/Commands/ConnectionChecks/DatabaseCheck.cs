using System;
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

            if (configuration is null)
            {
                output.Line("Effective Configuration is null - see previous checks", 1);
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
                    var transactions = await context.Database.GetAppliedMigrationsAsync();

                    output.Line($"found '{transactions.Count()}' transactions");
                }
            }
            catch (Exception e)
            {
                output.Line($"Error while querying database: {e.GetType().Name}; {e.Message}");
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

        /// <inheritdoc />
        public DatabaseCheckContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}