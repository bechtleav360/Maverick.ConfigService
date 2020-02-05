using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Common.DbContexts
{
    public class SqliteSnapshotContext : SnapshotContext
    {
        /// <inheritdoc />
        public SqliteSnapshotContext(DbContextOptions options) : base(options)
        {
        }
    }
}