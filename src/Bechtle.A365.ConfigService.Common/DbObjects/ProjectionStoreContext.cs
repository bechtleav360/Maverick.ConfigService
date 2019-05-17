using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Common.DbObjects
{
    public sealed class ProjectionStoreContext : DbContext
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
        public IQueryable<ConfigEnvironmentKeyPath> FullAutoCompletePaths => AutoCompletePaths.Include(p => p.Parent)
                                                                                              .Include(p => p.Children)
                                                                                              .Include(p => p.ConfigEnvironment);

        /// <summary>
        /// </summary>
        public IQueryable<ConfigEnvironment> FullConfigEnvironments => ConfigEnvironments.Include(env => env.Keys);

        /// <summary>
        /// </summary>
        public IQueryable<ProjectedConfiguration> FullProjectedConfigurations => ProjectedConfigurations.Include(c => c.Keys)
                                                                                                        .Include(c => c.UsedConfigurationKeys)
                                                                                                        .Include(c => c.ConfigEnvironment)
                                                                                                        .Include(c => c.Structure);

        /// <summary>
        /// </summary>
        public IQueryable<Structure> FullStructures => Structures.Include(s => s.Keys)
                                                                 .Include(s => s.Variables);

        /// <summary>
        /// </summary>
        public DbSet<ProjectionMetadata> Metadata { get; set; }

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

            modelBuilder.Entity<ProjectionMetadata>();

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