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
        public DbSet<ConfigEnvironmentKeyPath> AutoCompletePaths { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ConfigEnvironmentKey> ConfigEnvironmentKeys { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ProjectedConfigurationKey> ProjectedConfigurationKeys { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<StructureKey> StructureKeys { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<Structure> Structures { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Structure>();

            modelBuilder.Entity<StructureKey>()
                        .ToTable(nameof(StructureKey));

            modelBuilder.Entity<StructureVariable>()
                        .ToTable(nameof(StructureVariable));

            modelBuilder.Entity<ConfigEnvironment>();

            modelBuilder.Entity<ConfigEnvironmentKey>()
                        .ToTable(nameof(ConfigEnvironmentKey));

            modelBuilder.Entity<UsedConfigurationKey>()
                        .ToTable(nameof(UsedConfigurationKey));

            modelBuilder.Entity<ProjectedConfiguration>();

            modelBuilder.Entity<ProjectedConfigurationKey>()
                        .ToTable(nameof(ProjectedConfigurationKey));

            modelBuilder.Entity<ConfigEnvironmentKeyPath>();
        }
    }
}