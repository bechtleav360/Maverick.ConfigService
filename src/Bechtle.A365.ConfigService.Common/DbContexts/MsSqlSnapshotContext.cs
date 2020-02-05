using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Common.DbContexts
{
    public class MsSqlSnapshotContext: SnapshotContext
    {
        /// <inheritdoc />
        public MsSqlSnapshotContext(DbContextOptions options) : base(options)
        {
        }
    }
}