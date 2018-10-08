using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class ConfigurationDatabase : IConfigurationDatabase
    {
        private readonly ProjectionStorageConfiguration _config;
        private readonly ILogger<ConfigurationDatabase> _logger;

        public ConfigurationDatabase(ILogger<ConfigurationDatabase> logger,
                                     ProjectionStorageConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        /// <inheritdoc />
        public async Task<Result> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            using (var context = OpenProjectionStore())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment is null)
                {
                    _logger.LogError($"could not find environment {identifier} to apply modifications");
                    return Result.Error($"could not find environment {identifier} to apply modifications", ErrorCode.NotFound);
                }

                foreach (var action in actions)
                    switch (action.Type)
                    {
                        case ConfigKeyActionType.Set:
                        {
                            var existingKey = environment.Keys.FirstOrDefault(k => k.Key == action.Key);

                            if (existingKey is null)
                                environment.Keys.Add(new ConfigEnvironmentKey
                                {
                                    Id = Guid.NewGuid(),
                                    ConfigEnvironmentId = environment.Id,
                                    Key = action.Key,
                                    Value = action.Value
                                });
                            else
                                existingKey.Value = action.Value;

                            break;
                        }

                        case ConfigKeyActionType.Delete:
                        {
                            var existingKey = environment.Keys.FirstOrDefault(k => k.Key == action.Key);
                            if (existingKey is null)
                            {
                                _logger.LogError($"could not remove key '{action.Key}' from environment {identifier}: not found");
                                return Result.Error($"could not remove key '{action.Key}' from environment {identifier}", ErrorCode.NotFound);
                            }

                            environment.Keys.Remove(existingKey);
                            break;
                        }

                        default:
                            _logger.LogCritical($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'");
                            return Result.Error($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'", ErrorCode.InvalidData);
                    }

                try
                {
                    await context.SaveChangesAsync();
                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not apply actions to environment {identifier}: {e}");
                    return Result.Error($"could not apply actions to environment {identifier}: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result> Connect()
        {
            using (var connection = OpenProjectionStore())
            {
                await connection.Database.EnsureCreatedAsync();

                return Result.Success();
            }
        }

        /// <inheritdoc />
        public async Task<Result> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment)
        {
            using (var context = OpenProjectionStore())
            {
                if (await GetEnvironmentInternal(identifier, context) != null)
                {
                    _logger.LogError($"environment with id {identifier} already exists");
                    return Result.Error($"environment with id {identifier} already exists", ErrorCode.EnvironmentAlreadyExists);
                }

                // additional check to make sure there is only ever one default-environment per category
                if (defaultEnvironment)
                {
                    var defaultEnvironments = context.ConfigEnvironments.Count(env => string.Equals(env.Category,
                                                                                                    identifier.Category,
                                                                                                    StringComparison.OrdinalIgnoreCase)
                                                                                      && env.DefaultEnvironment);

                    if (defaultEnvironments > 0)
                    {
                        _logger.LogError($"can not create another default-environment in category '{identifier.Category}'");
                        return Result.Error($"can not create another default-environment in category '{identifier.Category}'",
                                            ErrorCode.DefaultEnvironmentAlreadyExists);
                    }
                }

                await context.ConfigEnvironments.AddAsync(new ConfigEnvironment
                {
                    Id = Guid.NewGuid(),
                    Name = identifier.Name,
                    Category = identifier.Category,
                    DefaultEnvironment = defaultEnvironment,
                    Keys = new List<ConfigEnvironmentKey>()
                });

                try
                {
                    await context.SaveChangesAsync();

                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not save new Environment {identifier} to database: {e}");
                    return Result.Error($"could not save new Environment {identifier} to database: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result> CreateStructure(StructureIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            using (var context = OpenProjectionStore())
            {
                if (await GetStructureInternal(identifier, context) != null)
                {
                    _logger.LogError($"structure with id {identifier} already exists");
                    return Result.Error($"structure with id {identifier} already exists", ErrorCode.StructureAlreadyExists);
                }

                await context.Structures.AddAsync(new Structure
                {
                    Id = Guid.NewGuid(),
                    Name = identifier.Name,
                    Version = identifier.Version,
                    Keys = actions.Where(action => action.Type == ConfigKeyActionType.Set)
                                  .Select(action => new StructureKey
                                  {
                                      Id = Guid.NewGuid(),
                                      Key = action.Key,
                                      Value = action.Value
                                  })
                                  .ToList()
                });

                try
                {
                    await context.SaveChangesAsync();

                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not save new Structure {identifier} to database: {e}");
                    return Result.Error($"could not save new Structure {identifier} to database: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteEnvironment(EnvironmentIdentifier identifier)
        {
            using (var context = OpenProjectionStore())
            {
                var foundEnvironment = await GetEnvironmentInternal(identifier, context);

                if (foundEnvironment is null)
                    return Result.Success();

                context.ConfigEnvironments.Remove(foundEnvironment);

                try
                {
                    await context.SaveChangesAsync();

                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not delete environment {identifier} from database: {e}");
                    return Result.Error($"could not delete environment {identifier} from database: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteStructure(StructureIdentifier identifier)
        {
            using (var context = OpenProjectionStore())
            {
                var foundStructure = await GetStructureInternal(identifier, context);

                if (foundStructure is null)
                    return Result.Success();

                context.Structures.Remove(foundStructure);

                try
                {
                    await context.SaveChangesAsync();

                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not delete Structure {identifier} from database: {e}");
                    return Result.Error($"could not delete Structure {identifier} from database: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result<Snapshot<EnvironmentIdentifier>>> GetDefaultEnvironment(string category)
            => await GetEnvironment(new EnvironmentIdentifier(category, "Default"));

        /// <inheritdoc />
        public async Task<Result<Snapshot<EnvironmentIdentifier>>> GetEnvironment(EnvironmentIdentifier identifier)
        {
            using (var context = OpenProjectionStore())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment == null)
                {
                    _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                    return Result<Snapshot<EnvironmentIdentifier>>.Error($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
                }

                var environmentData = environment.Keys
                                                 .ToDictionary(data => data.Key,
                                                               data => data.Value);

                return Result.Success(new Snapshot<EnvironmentIdentifier>(identifier,
                                                                          1,
                                                                          environmentData));
            }
        }

        public async Task<Result<Snapshot<EnvironmentIdentifier>>> GetEnvironmentWithInheritance(EnvironmentIdentifier identifier)
        {
            using (var context = OpenProjectionStore())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment is null)
                {
                    _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                    return Result<Snapshot<EnvironmentIdentifier>>.Error($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
                }

                var environmentData = environment.Keys
                                                 .ToDictionary(data => data.Key,
                                                               data => data.Value);

                var defaultEnv = await GetEnvironmentInternal(new EnvironmentIdentifier(identifier.Category, "Default"), context);

                // add all keys from defaultEnv to environmentData that are not already set in environmentData
                // inherit by adding instead of overriding keys
                if (defaultEnv is null)
                    _logger.LogWarning($"no default-environment found for category '{identifier.Category}'");
                else
                    foreach (var kvp in defaultEnv.Keys)
                        if (!environmentData.ContainsKey(kvp.Key))
                            environmentData[kvp.Key] = kvp.Value;

                return Result.Success(new Snapshot<EnvironmentIdentifier>(identifier,
                                                                          1,
                                                                          environmentData));
            }
        }

        /// <inheritdoc />
        public async Task<long?> GetLatestProjectedEventId()
        {
            using (var context = OpenProjectionStore())
            {
                var metadata = await context.Metadata.FirstOrDefaultAsync();

                if (metadata is null)
                {
                    metadata = new ProjectionMetadata();
                    await context.Metadata.AddAsync(metadata);
                    await context.SaveChangesAsync();
                }

                return metadata.LatestEvent;
            }
        }

        /// <inheritdoc />
        public async Task<Result<Snapshot<StructureIdentifier>>> GetStructure(StructureIdentifier identifier)
        {
            using (var context = OpenProjectionStore())
            {
                var structure = await GetStructureInternal(identifier, context);

                if (structure == null)
                {
                    _logger.LogError($"no {nameof(Structure)} with id {identifier} found");
                    return Result<Snapshot<StructureIdentifier>>.Error($"no {nameof(Structure)} with id {identifier} found", ErrorCode.NotFound);
                }

                return Result.Success(new Snapshot<StructureIdentifier>(identifier,
                                                                        structure.Version,
                                                                        structure.Keys
                                                                                 .ToDictionary(data => data.Key,
                                                                                               data => data.Value)));
            }
        }

        /// <inheritdoc />
        public async Task<Result> SaveConfiguration(Snapshot<EnvironmentIdentifier> environment,
                                                    Snapshot<StructureIdentifier> structure,
                                                    IDictionary<string, string> configuration,
                                                    string configurationJson,
                                                    DateTime? validFrom,
                                                    DateTime? validTo)
        {
            using (var context = OpenProjectionStore())
            {
                var foundEnvironment = await GetEnvironmentInternal(environment.Identifier, context);

                // version is already included in structure.Identifier, as opposed to environment.Identifier
                var foundStructure = await GetStructureInternal(structure.Identifier, context);

                var compiledConfiguration = new ProjectedConfiguration
                {
                    Id = Guid.NewGuid(),
                    ConfigEnvironmentId = foundEnvironment.Id,
                    StructureId = foundStructure.Id,
                    StructureVersion = foundStructure.Version,
                    ConfigurationJson = configurationJson,
                    ValidFrom = validFrom,
                    ValidTo = validTo,
                    Keys = configuration.Select(kvp => new ProjectedConfigurationKey
                                        {
                                            Id = Guid.NewGuid(),
                                            Key = kvp.Key,
                                            Value = kvp.Value
                                        })
                                        .ToList()
                };

                var existingConfiguration = await context.ProjectedConfigurations
                                                         .FirstOrDefaultAsync(c => c.ConfigEnvironment.Category == environment.Identifier.Category &&
                                                                                   c.ConfigEnvironment.Name == environment.Identifier.Name &&
                                                                                   c.Structure.Name == structure.Identifier.Name &&
                                                                                   c.Structure.Version == structure.Identifier.Version);

                try
                {
                    if (existingConfiguration != null)
                    {
                        context.ProjectedConfigurations.Remove(existingConfiguration);
                        await context.SaveChangesAsync();
                    }

                    await context.ProjectedConfigurations.AddAsync(compiledConfiguration);

                    await context.SaveChangesAsync();

                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not save compiled configuration: {e}");
                    return Result.Error($"could not save compiled configuration: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task SetLatestProjectedEventId(long latestEventId)
        {
            using (var context = OpenProjectionStore())
            {
                var metadata = await context.Metadata.FirstOrDefaultAsync();

                if (metadata is null)
                {
                    metadata = new ProjectionMetadata();
                    await context.Metadata.AddAsync(metadata);
                    await context.SaveChangesAsync();
                }

                metadata.LatestEvent = latestEventId;
                await context.SaveChangesAsync();
            }
        }

        private async Task<ConfigEnvironment> GetEnvironmentInternal(EnvironmentIdentifier identifier, ProjectionStore context)
        {
            return await context.ConfigEnvironments.FirstOrDefaultAsync(env => env.Category == identifier.Category &&
                                                                               env.Name == identifier.Name);
        }

        private async Task<Structure> GetStructureInternal(StructureIdentifier identifier, ProjectionStore context)
        {
            return await context.Structures.FirstOrDefaultAsync(str => str.Name == identifier.Name &&
                                                                       str.Version == identifier.Version);
        }

        private ProjectionStore OpenProjectionStore(ProjectionStore existingConnection = null)
            => existingConnection ?? new ProjectionStore(_config);

        // property accessors are actually required for EFCore
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class ProjectionStore : DbContext
        {
            private readonly ProjectionStorageConfiguration _config;

            public ProjectionStore(ProjectionStorageConfiguration config)
            {
                _config = config;
            }

            public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

            public DbSet<ProjectionMetadata> Metadata { get; set; }

            public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

            public DbSet<Structure> Structures { get; set; }

            /// <inheritdoc />
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseLazyLoadingProxies()
                                 .UseSqlServer(_config.ConnectionString);

            /// <inheritdoc />
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<ProjectionMetadata>();

                modelBuilder.Entity<Structure>();
                modelBuilder.Entity<StructureKey>();
                modelBuilder.Entity<ConfigEnvironment>();
                modelBuilder.Entity<ConfigEnvironmentKey>();
                modelBuilder.Entity<ProjectedConfiguration>();
                modelBuilder.Entity<ProjectedConfigurationKey>();
            }
        }

        public class ProjectionMetadata
        {
            public Guid Id { get; set; }

            /// <summary>
            ///     null to indicate that no events have been projected
            /// </summary>
            public long? LatestEvent { get; set; }
        }
    }
}