using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.Common.Utilities.Extensions;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class ConfigurationDatabase : IConfigurationDatabase
    {
        private readonly ILogger<ConfigurationDatabase> _logger;
        private readonly IServiceProvider _provider;

        public ConfigurationDatabase(ILogger<ConfigurationDatabase> logger,
                                     IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;

            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                context.Database.Migrate();
            }
        }

        /// <inheritdoc />
        public async Task<Result> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment is null)
                {
                    _logger.LogError($"could not find environment {identifier} to apply modifications");
                    return Result.Error($"could not find environment {identifier} to apply modifications", ErrorCode.NotFound);
                }

                // 'look-up table' string=>ConfigEnvironmentKey to prevent searching the entire list for each and every key
                var keyDict = environment.Keys.ToDictionary(k => k.Key, k => k);

                // changes we want to make later on
                // many small changes to EF-List environment.Keys will result in abysmal performance
                var addedKeys = new List<ConfigEnvironmentKey>();
                var removedKeys = new List<ConfigEnvironmentKey>();

                foreach (var action in actions)
                    switch (action.Type)
                    {
                        case ConfigKeyActionType.Set:
                        {
                            var existingKey = keyDict.ContainsKey(action.Key)
                                                  ? keyDict[action.Key]
                                                  : null;

                            if (existingKey is null)
                                addedKeys.Add(new ConfigEnvironmentKey
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
                            var existingKey = keyDict.ContainsKey(action.Key)
                                                  ? keyDict[action.Key]
                                                  : null;

                            if (existingKey is null)
                            {
                                _logger.LogError($"could not remove key '{action.Key}' from environment {identifier}: not found");
                                return Result.Error($"could not remove key '{action.Key}' from environment {identifier}", ErrorCode.NotFound);
                            }

                            removedKeys.Add(existingKey);
                            break;
                        }

                        default:
                            _logger.LogCritical($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'");
                            return Result.Error($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'", ErrorCode.InvalidData);
                    }

                try
                {
                    if (removedKeys.Any())
                        environment.Keys.RemoveRange(removedKeys);

                    if (addedKeys.Any())
                        environment.Keys.AddRange(addedKeys);

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
        public async Task<Result> ApplyChanges(StructureIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var structure = await GetStructureInternal(identifier, context);

                if (structure is null)
                {
                    _logger.LogError($"could not find structure {identifier} to apply modifications");
                    return Result.Error($"could not find structure {identifier} to apply modifications", ErrorCode.NotFound);
                }

                // 'look-up table' string=>StructureVariable to prevent searching the entire list for each and every key
                var keyDict = structure.Variables.ToDictionary(k => k.Key, k => k);

                // changes we want to make later on
                // many small changes to EF-List environment.Keys will result in abysmal performance
                var addedKeys = new List<StructureVariable>();
                var removedKeys = new List<StructureVariable>();

                foreach (var action in actions)
                    switch (action.Type)
                    {
                        case ConfigKeyActionType.Set:
                        {
                            var existingKey = keyDict.ContainsKey(action.Key)
                                                  ? keyDict[action.Key]
                                                  : null;

                            if (existingKey is null)
                                addedKeys.Add(new StructureVariable
                                {
                                    Id = Guid.NewGuid(),
                                    StructureId = structure.Id,
                                    Key = action.Key,
                                    Value = action.Value
                                });
                            else
                                existingKey.Value = action.Value;

                            break;
                        }

                        case ConfigKeyActionType.Delete:
                        {
                            var existingKey = keyDict.ContainsKey(action.Key)
                                                  ? keyDict[action.Key]
                                                  : null;

                            if (existingKey is null)
                            {
                                _logger.LogError($"could not remove variable '{action.Key}' from structure {identifier}: not found");
                                return Result.Error($"could not remove variable '{action.Key}' from structure {identifier}", ErrorCode.NotFound);
                            }

                            removedKeys.Add(existingKey);
                            break;
                        }

                        default:
                            _logger.LogCritical($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'");
                            return Result.Error($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'", ErrorCode.InvalidData);
                    }

                try
                {
                    if (removedKeys.Any())
                        structure.Variables.RemoveRange(removedKeys);

                    if (addedKeys.Any())
                        structure.Variables.AddRange(addedKeys);

                    await context.SaveChangesAsync();
                    return Result.Success();
                }
                catch (DbUpdateException e)
                {
                    _logger.LogError($"could not apply actions to structure {identifier}: {e}");
                    return Result.Error($"could not apply actions to structure {identifier}: {e}", ErrorCode.DbUpdateError);
                }
            }
        }

        /// <inheritdoc />
        public async Task<Result> Connect()
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                await context.Database.EnsureCreatedAsync();

                return Result.Success();
            }
        }

        /// <inheritdoc />
        public async Task<Result> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
        public async Task<Result> CreateStructure(StructureIdentifier identifier,
                                                  IDictionary<string, string> keys,
                                                  IDictionary<string, string> variables)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
                    Keys = keys.Select(kvp => new StructureKey
                               {
                                   Id = Guid.NewGuid(),
                                   Key = kvp.Key,
                                   Value = kvp.Value
                               })
                               .ToList(),
                    Variables = variables.Select(kvp => new StructureVariable
                                         {
                                             Id = Guid.NewGuid(),
                                             Key = kvp.Key,
                                             Value = kvp.Value
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
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
        public async Task<Result<EnvironmentSnapshot>> GetDefaultEnvironment(string category)
            => await GetEnvironment(new EnvironmentIdentifier(category, "Default"));

        /// <inheritdoc />
        public async Task<Result<EnvironmentSnapshot>> GetEnvironment(EnvironmentIdentifier identifier)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment == null)
                {
                    _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                    return Result<EnvironmentSnapshot>.Error($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
                }

                var environmentData = environment.Keys
                                                 .ToDictionary(data => data.Key,
                                                               data => data.Value);

                return Result.Success(new EnvironmentSnapshot(identifier, environmentData));
            }
        }

        /// <inheritdoc />
        public async Task<Result<EnvironmentSnapshot>> GetEnvironmentWithInheritance(EnvironmentIdentifier identifier)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var environment = await GetEnvironmentInternal(identifier, context);

                if (environment is null)
                {
                    _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                    return Result<EnvironmentSnapshot>.Error($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
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

                return Result.Success(new EnvironmentSnapshot(identifier, environmentData));
            }
        }

        /// <inheritdoc />
        public async Task<long?> GetLatestProjectedEventId()
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
        public async Task<Result<StructureSnapshot>> GetStructure(StructureIdentifier identifier)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var structure = await GetStructureInternal(identifier, context);

                if (structure == null)
                {
                    _logger.LogError($"no {nameof(Structure)} with id {identifier} found");
                    return Result<StructureSnapshot>.Error($"no {nameof(Structure)} with id {identifier} found", ErrorCode.NotFound);
                }

                return Result.Success(new StructureSnapshot(identifier,
                                                            structure.Version,
                                                            structure.Keys
                                                                     .ToDictionary(data => data.Key,
                                                                                   data => data.Value),
                                                            structure.Variables
                                                                     .ToDictionary(data => data.Key,
                                                                                   data => data.Value)));
            }
        }

        /// <inheritdoc />
        public async Task<Result> SaveConfiguration(EnvironmentSnapshot environment,
                                                    StructureSnapshot structure,
                                                    IDictionary<string, string> configuration,
                                                    string configurationJson,
                                                    IEnumerable<string> usedKeys,
                                                    DateTime? validFrom,
                                                    DateTime? validTo)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
                    UsedConfigurationKeys = usedKeys.Where(k => environment.Data.ContainsKey(k))
                                                    .Select(key => new UsedConfigurationKey
                                                    {
                                                        Id = Guid.NewGuid(),
                                                        Key = key
                                                    })
                                                    .ToList(),
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
                                                                                   c.Structure.Version == structure.Identifier.Version &&
                                                                                   c.ValidFrom == validFrom &&
                                                                                   c.ValidTo == validTo);

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
        public async Task<ConfigurationIdentifier> GetLatestActiveConfiguration()
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var metadata = await context.Metadata.FirstOrDefaultAsync();

                if (metadata is null)
                {
                    metadata = new ProjectionMetadata();
                    await context.Metadata.AddAsync(metadata);
                    await context.SaveChangesAsync();
                }

                var configId = metadata.LastActiveConfigurationId;
                if (configId == Guid.Empty)
                    return null;

                var configuration = await context.ProjectedConfigurations
                                                 .Where(c => c.Id == configId)
                                                 .Select(c => new ConfigurationIdentifier(
                                                             new EnvironmentIdentifier(c.ConfigEnvironment.Category, c.ConfigEnvironment.Name),
                                                             new StructureIdentifier(c.Structure.Name, c.Structure.Version)))
                                                 .FirstOrDefaultAsync();

                return configuration;
            }
        }

        /// <inheritdoc />
        public async Task SetLatestActiveConfiguration(ConfigurationIdentifier identifier)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
            {
                var metadata = await context.Metadata.FirstOrDefaultAsync();

                if (metadata is null)
                {
                    metadata = new ProjectionMetadata();
                    await context.Metadata.AddAsync(metadata);
                    await context.SaveChangesAsync();
                }

                var configuration = await context.ProjectedConfigurations
                                                 .Where(c => c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                             c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                             c.Structure.Name == identifier.Structure.Name &&
                                                             c.Structure.Version == identifier.Structure.Version)
                                                 .Select(c => c.Id)
                                                 .FirstOrDefaultAsync();

                metadata.LastActiveConfigurationId = configuration;

                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task SetLatestProjectedEventId(long latestEventId)
        {
            using (var scope = _provider.CreateScope())
            using (var context = scope.ServiceProvider.GetService<ProjectionStore>())
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
    }
}