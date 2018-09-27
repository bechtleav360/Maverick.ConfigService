using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class DebugConfigurationDatabase : IConfigurationDatabase, IDisposable
    {
        private readonly Context _context;
        private readonly ILogger<DebugConfigurationDatabase> _logger;

        public DebugConfigurationDatabase(ILogger<DebugConfigurationDatabase> logger)
        {
            _logger = logger;
            _context = new Context();
            _context.Database.EnsureCreated();
        }

        /// <inheritdoc />
        public async Task<Result> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            var environment = await GetEnvironmentInternal(identifier);

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
                            environment.Keys.Add(new EnvironmentKey
                            {
                                Id = Guid.NewGuid(),
                                EnvironmentId = environment.Id,
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
                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not apply actions to environment {identifier}: {e}");
                return Result.Error($"could not apply actions to environment {identifier}: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<Result> Connect() => Task.FromResult(Result.Success());

        /// <inheritdoc />
        public async Task<Result> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment)
        {
            if (await GetEnvironmentInternal(identifier) != null)
            {
                _logger.LogError($"environment with id {identifier} already exists");
                return Result.Error($"environment with id {identifier} already exists", ErrorCode.EnvironmentAlreadyExists);
            }

            // additional check to make sure there is only ever one default-environment per category
            if (defaultEnvironment)
            {
                var defaultEnvironments = _context.Environments.Count(env => string.Equals(env.Category,
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

            await _context.Environments.AddAsync(new Environment
            {
                Id = Guid.NewGuid(),
                Name = identifier.Name,
                Category = identifier.Category,
                Version = 1,
                DefaultEnvironment = defaultEnvironment,
                Keys = new List<EnvironmentKey>()
            });

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save new Environment {identifier} to database: {e}");
                return Result.Error($"could not save new Environment {identifier} to database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> CreateStructure(StructureIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            if (await GetStructureInternal(identifier) != null)
            {
                _logger.LogError($"structure with id {identifier} already exists");
                return Result.Error($"structure with id {identifier} already exists", ErrorCode.StructureAlreadyExists);
            }

            await _context.Structures.AddAsync(new Structure
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
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save new Structure {identifier} to database: {e}");
                return Result.Error($"could not save new Structure {identifier} to database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteEnvironment(EnvironmentIdentifier identifier)
        {
            var foundEnvironment = await GetEnvironmentInternal(identifier);

            if (foundEnvironment is null)
                return Result.Success();

            _context.Environments.Remove(foundEnvironment);

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete environment {identifier} from database: {e}");
                return Result.Error($"could not delete environment {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteStructure(StructureIdentifier identifier)
        {
            var foundStructure = await GetStructureInternal(identifier);

            if (foundStructure is null)
                return Result.Success();

            _context.Structures.Remove(foundStructure);

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete Structure {identifier} from database: {e}");
                return Result.Error($"could not delete Structure {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<Snapshot<EnvironmentIdentifier>>> GetEnvironment(EnvironmentIdentifier identifier)
        {
            var environment = await GetEnvironmentInternal(identifier);

            if (environment == null)
            {
                _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                return Result<Snapshot<EnvironmentIdentifier>>.Error($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
            }

            var environmentData = environment.Keys
                                             .ToDictionary(data => data.Key,
                                                           data => data.Value);

            var defaultEnvironment = await _context.Environments.FirstOrDefaultAsync(env => string.Equals(env.Category,
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

        /// <inheritdoc />
        public async Task<Result<Snapshot<StructureIdentifier>>> GetStructure(StructureIdentifier identifier)
        {
            var structure = await GetStructureInternal(identifier);

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

        /// <inheritdoc />
        public async Task<Result> SaveConfiguration(Snapshot<EnvironmentIdentifier> environment,
                                                    Snapshot<StructureIdentifier> structure,
                                                    IDictionary<string, string> configuration)
        {
            var foundEnvironment = await GetEnvironmentInternal(environment.Identifier);

            // version is already included in structure.Identifier, as opposed to environment.Identifier
            var foundStructure = await GetStructureInternal(structure.Identifier);

            var compiledConfiguration = new Configuration
            {
                Id = Guid.NewGuid(),
                EnvironmentId = foundEnvironment.Id,
                EnvironmentVersion = foundEnvironment.Version,
                StructureId = foundStructure.Id,
                StructureVersion = foundStructure.Version,
                Keys = configuration.Select(kvp => new ConfigurationKey
                                    {
                                        Id = Guid.NewGuid(),
                                        Key = kvp.Key,
                                        Value = kvp.Value
                                    })
                                    .ToList()
            };

            await _context.Configurations.AddAsync(compiledConfiguration);

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save compiled configuration: {e}");
                return Result.Error($"could not save compiled configuration: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context?.Dispose();
        }

        private async Task<Environment> GetEnvironmentInternal(EnvironmentIdentifier identifier)
            => await _context.Environments.FirstOrDefaultAsync(env => env.Category == identifier.Category &&
                                                                      env.Name == identifier.Name);

        private async Task<Structure> GetStructureInternal(StructureIdentifier identifier)
            => await _context.Structures.FirstOrDefaultAsync(str => str.Name == identifier.Name &&
                                                                    str.Version == identifier.Version);

        private class Context : DbContext
        {
            public DbSet<Configuration> Configurations { get; set; }

            public DbSet<Environment> Environments { get; set; }

            public DbSet<Structure> Structures { get; set; }

            /// <inheritdoc />
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                base.OnConfiguring(optionsBuilder);

                optionsBuilder.UseSqlite("DataSource=./projected.db");
            }

            /// <inheritdoc />
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Environment>();
                modelBuilder.Entity<EnvironmentKey>();
                modelBuilder.Entity<Structure>();
                modelBuilder.Entity<StructureKey>();
                modelBuilder.Entity<Configuration>();
                modelBuilder.Entity<ConfigurationKey>();
            }
        }

        private class Configuration
        {
            public Guid EnvironmentId { get; set; }

            public int EnvironmentVersion { get; set; }

            public Guid Id { get; set; }

            public List<ConfigurationKey> Keys { get; set; }

            public Guid StructureId { get; set; }

            public int StructureVersion { get; set; }
        }

        private class ConfigurationKey
        {
            public Guid ConfigurationId { get; set; }

            public Guid Id { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }
        }

        private class Environment
        {
            public string Category { get; set; }

            public bool DefaultEnvironment { get; set; }

            public Guid Id { get; set; }

            public List<EnvironmentKey> Keys { get; set; }

            public string Name { get; set; }

            public int Version { get; set; }
        }

        private class EnvironmentKey
        {
            public Guid EnvironmentId { get; set; }

            public Guid Id { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }
        }

        private class Structure
        {
            public Guid Id { get; set; }

            public List<StructureKey> Keys { get; set; }

            public string Name { get; set; }

            public int Version { get; set; }
        }

        private class StructureKey
        {
            public Guid Id { get; set; }

            public string Key { get; set; }

            public Guid StructureId { get; set; }

            public string Value { get; set; }
        }
    }
}