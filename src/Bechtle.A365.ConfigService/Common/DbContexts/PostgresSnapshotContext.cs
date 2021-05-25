using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Common.DbContexts
{
    public class PostgresSnapshotContext : SnapshotContext
    {
        /// <inheritdoc />
        public PostgresSnapshotContext(DbContextOptions options) : base(options)
        {
        }
    }
}