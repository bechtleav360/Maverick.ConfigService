using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Common.DbContexts
{
    /// <summary>
    ///     somewhat generic DbContext to store snapshot-data using an ef-core Database
    /// </summary>
    public class SnapshotContext : DbContext
    {
        /// <inheritdoc />
        public SnapshotContext(DbContextOptions options) : base(options)
        {
        }

        /// <summary>
        ///     public <see cref="DbSet{TSnapshot}" /> that can be used to store snapshots in the configured Database
        /// </summary>
        public DbSet<SqlSnapshot> Snapshots { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ConfigService");

            modelBuilder.Entity<SqlSnapshot>().HasKey(e => e.Id);

            base.OnModelCreating(modelBuilder);
        }
    }
}