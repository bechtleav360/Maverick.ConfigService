using Bechtle.A365.ConfigService.Common.DbObjects;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class ProjectionStoreContext : DbContext
    {
        /// <inheritdoc />
        public ProjectionStoreContext(DbContextOptions<ProjectionStoreContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// </summary>
        public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<Structure> Structures { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Structure>();
            modelBuilder.Entity<StructureKey>();
            modelBuilder.Entity<StructureVariable>();
            modelBuilder.Entity<ConfigEnvironment>();
            modelBuilder.Entity<ConfigEnvironmentKey>();
            modelBuilder.Entity<UsedConfigurationKey>();
            modelBuilder.Entity<ProjectedConfiguration>();
            modelBuilder.Entity<ProjectedConfigurationKey>();
        }
    }
}