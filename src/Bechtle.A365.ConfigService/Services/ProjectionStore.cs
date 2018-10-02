using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Dto.DbObjects;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc cref="DbContext"/>
    /// <inheritdoc cref="IProjectionStore"/>
    public class ProjectionStore : DbContext, IProjectionStore
    {
        private readonly ProjectionStorageConfiguration _config;

        /// <summary>
        /// </summary>
        /// <param name="config"></param>
        public ProjectionStore(ProjectionStorageConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// </summary>
        public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

        /// <summary>
        /// </summary>
        public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

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

        /// <inheritdoc />
        public async Task<IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>> GetAvailableConfigurations()
            // order result-set first
            => (await ProjectedConfigurations.OrderBy(c => c.ConfigEnvironment.Category)
                                             .ThenBy(c => c.ConfigEnvironment.Name)
                                             .ThenBy(c => c.Structure.Name)
                                             .ThenByDescending(c => c.StructureVersion)
                                             .ToListAsync())
               // group all returned structures by the Environment.Category and Environment.Name of the linked Configuration
               .GroupBy(c => new {c.ConfigEnvironment.Category, c.ConfigEnvironment.Name},
                        c => new StructureIdentifier(c.Structure))
               // create a dictionary Env-ID => Struct-ID[]
               .ToDictionary(g => new EnvironmentIdentifier(g.Key.Category, g.Key.Name),
                             g => (IList<StructureIdentifier>) g.ToList());

        /// <inheritdoc />
        public async Task<ProjectedConfiguration> GetProjectedConfiguration(EnvironmentIdentifier environment, StructureIdentifier structure)
            => await ProjectedConfigurations.FirstOrDefaultAsync(configuration => configuration.ConfigEnvironment.Name == environment.Name &&
                                                                                  configuration.ConfigEnvironment.Category == environment.Category &&
                                                                                  configuration.Structure.Name == structure.Name &&
                                                                                  configuration.Structure.Version == structure.Version);

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> GetProjectedConfigurationKeys(EnvironmentIdentifier environment, StructureIdentifier structure)
            => (await GetProjectedConfiguration(environment, structure))
               ?.Keys
               .ToImmutableSortedDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase)
               ?? (IDictionary<string, string>) new Dictionary<string, string>();
    }
}