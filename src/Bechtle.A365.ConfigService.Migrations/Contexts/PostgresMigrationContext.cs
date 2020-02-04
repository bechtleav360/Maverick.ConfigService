using Bechtle.A365.ConfigService.Common.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Migrations.Contexts
{
    public class PostgresMigrationContext : PostgresSnapshotContext
    {
        /// <inheritdoc />
        public PostgresMigrationContext(DbContextOptions options) : base(options)
        {
        }
    }
}