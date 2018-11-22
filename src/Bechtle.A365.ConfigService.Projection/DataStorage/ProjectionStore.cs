using System.Diagnostics.CodeAnalysis;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    // property accessors are actually required for EFCore
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public sealed class ProjectionStore : DbContext
    {
        public ProjectionStore(DbContextOptions<ProjectionStore> options)
            : base(options)
        {
        }

        public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

        public DbSet<ProjectionMetadata> Metadata { get; set; }

        public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

        public DbSet<Structure> Structures { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProjectionMetadata>();

            modelBuilder.Entity<Structure>();
            modelBuilder.Entity<StructureKey>();
            modelBuilder.Entity<StructureVariable>();
            modelBuilder.Entity<ConfigEnvironment>();
            modelBuilder.Entity<ConfigEnvironmentKey>();
            modelBuilder.Entity<ProjectedConfiguration>();
            modelBuilder.Entity<ProjectedConfigurationKey>();
        }
    }
}