using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DbObjects;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
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
                    Version = 1,
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

                var defaultEnvironment = await context.ConfigEnvironments.FirstOrDefaultAsync(env => string.Equals(env.Category,
                                                                                                                   identifier.Category,
                                                                                                                   StringComparison.OrdinalIgnoreCase)
                                                                                                     && env.DefaultEnvironment);

                // early exit for when there is no default-environment
                if (defaultEnvironment == null)
                {
                    _logger.LogInformation($"no default-environment found for {identifier}, proceeding without default-environment");
                    return Result.Success(new Snapshot<EnvironmentIdentifier>(identifier,
                                                                              environment.Version,
                                                                              environmentData));
                }

                // gather default-environment data to a dictionary, and override its
                // keys with those that are present in the actual environment
                var completeData = defaultEnvironment.Keys
                                                     .ToDictionary(data => data.Key,
                                                                   data => data.Value);

                foreach (var kvp in environmentData)
                    completeData[kvp.Key] = kvp.Value;

                return Result.Success(new Snapshot<EnvironmentIdentifier>(identifier,
                                                                          environment.Version,
                                                                          completeData));
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
                                                    string configurationJson)
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
                    EnvironmentVersion = foundEnvironment.Version,
                    StructureId = foundStructure.Id,
                    StructureVersion = foundStructure.Version,
                    ConfigurationJson = configurationJson,
                    Keys = configuration.Select(kvp => new ProjectedConfigurationKey
                                        {
                                            Id = Guid.NewGuid(),
                                            Key = kvp.Key,
                                            Value = kvp.Value
                                        })
                                        .ToList()
                };

                await context.ProjectedConfigurations.AddAsync(compiledConfiguration);

                try
                {
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

        private class ProjectionStore : DbContext
        {
            private readonly ProjectionStorageConfiguration _config;

            public ProjectionStore(ProjectionStorageConfiguration config)
            {
                _config = config;
            }

            public DbSet<ProjectedConfiguration> ProjectedConfigurations { get; set; }

            public DbSet<ConfigEnvironment> ConfigEnvironments { get; set; }

            public DbSet<Structure> Structures { get; set; }

            /// <inheritdoc />
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseLazyLoadingProxies()
                                 .UseSqlServer(_config.ConnectionString);

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
}