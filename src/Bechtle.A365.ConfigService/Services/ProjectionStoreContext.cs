using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public sealed class ProjectionStoreContext : DbContext
    {
        private readonly ProjectionStorageConfiguration _config;

        /// <inheritdoc />
        public ProjectionStoreContext(ProjectionStorageConfiguration config)
        {
            _config = config;
            Database.EnsureCreated();
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
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer(_config.ConnectionString)
                          .UseLazyLoadingProxies();
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Structure>();
            modelBuilder.Entity<StructureKey>();
            modelBuilder.Entity<ConfigEnvironment>();
            modelBuilder.Entity<ConfigEnvironmentKey>();
            modelBuilder.Entity<ProjectedConfiguration>();
            modelBuilder.Entity<ProjectedConfigurationKey>();
        }
    }
}