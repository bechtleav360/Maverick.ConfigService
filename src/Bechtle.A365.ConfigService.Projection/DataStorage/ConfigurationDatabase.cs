using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.Common.Utilities.Extensions;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class ConfigurationDatabase : IConfigurationDatabase
    {
        private readonly ProjectionStore _context;
        private readonly ILogger<ConfigurationDatabase> _logger;

        public ConfigurationDatabase(ILogger<ConfigurationDatabase> logger, ProjectionStore context)
        {
            _logger = logger;
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IResult> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            var environment = await GetEnvironmentInternal(identifier);

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
            var changedKeys = new List<ConfigEnvironmentKey>();

            foreach (var action in actions)
                switch (action.Type)
                {
                    case ConfigKeyActionType.Set:
                    {
                        var existingKey = keyDict.ContainsKey(action.Key)
                                              ? keyDict[action.Key]
                                              : null;

                        if (existingKey is null)
                        {
                            addedKeys.Add(new ConfigEnvironmentKey
                            {
                                Id = Guid.NewGuid(),
                                ConfigEnvironmentId = environment.Id,
                                Key = action.Key,
                                Value = action.Value,
                                Description = action.Description,
                                Type = action.ValueType
                            });
                        }
                        else
                        {
                            existingKey.Value = action.Value;
                            existingKey.Description = action.Description;
                            existingKey.Type = action.ValueType;

                            changedKeys.Add(existingKey);
                        }

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

            // mark configurations as changed when:
            // UsedKeys contains one of the Changed / Deleted Keys

            foreach (var builtConfiguration in _context.FullProjectedConfigurations
                                                       .Where(c => c.UpToDate)
                                                       .ToArray())
            {
                var usedKeys = builtConfiguration.UsedConfigurationKeys
                                                 .OrderBy(k => k.Key)
                                                 .ToArray();

                // if any of the Changed- or Removed-Keys is found in the Keys used to build this Configuration - mark it as stale
                if (changedKeys.Select(ck => ck.Key)
                               .Any(ck => usedKeys.Select(uk => uk.Key)
                                                  .Contains(ck)) ||
                    removedKeys.Select(ck => ck.Key)
                               .Any(ck => usedKeys.Select(uk => uk.Key)
                                                  .Contains(ck)))
                    builtConfiguration.UpToDate = false;
            }

            try
            {
                if (removedKeys.Any())
                    environment.Keys.RemoveRange(removedKeys);

                if (addedKeys.Any())
                    environment.Keys.AddRange(addedKeys);

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not apply actions to environment {identifier}: {e}");
                return Result.Error($"could not apply actions to environment {identifier}: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> ApplyChanges(StructureIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            var structure = await GetStructureInternal(identifier);

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


                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not apply actions to structure {identifier}: {e}");
                return Result.Error($"could not apply actions to structure {identifier}: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> Connect()
        {
            await _context.Database.MigrateAsync();
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult> CreateEnvironment(EnvironmentIdentifier identifier, bool defaultEnvironment)
        {
            if (await GetEnvironmentInternal(identifier) != null)
            {
                _logger.LogError($"environment with id {identifier} already exists");
                return Result.Error($"environment with id {identifier} already exists", ErrorCode.EnvironmentAlreadyExists);
            }

            // additional check to make sure there is only ever one default-environment per category
            if (defaultEnvironment)
            {
                var defaultEnvironments = _context.ConfigEnvironments
                                                  .Count(env => string.Equals(env.Category,
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

            await _context.ConfigEnvironments.AddAsync(new ConfigEnvironment
            {
                Id = Guid.NewGuid(),
                Name = identifier.Name,
                Category = identifier.Category,
                DefaultEnvironment = defaultEnvironment,
                Keys = new List<ConfigEnvironmentKey>()
            });

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult> CreateStructure(StructureIdentifier identifier,
                                                   IDictionary<string, string> keys,
                                                   IDictionary<string, string> variables)
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
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save new Structure {identifier} to database: {e}");
                return Result.Error($"could not save new Structure {identifier} to database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteEnvironment(EnvironmentIdentifier identifier)
        {
            var foundEnvironment = await GetEnvironmentInternal(identifier);

            if (foundEnvironment is null)
                return Result.Success();

            _context.ConfigEnvironmentKeys
                    .RemoveRange(_context.ConfigEnvironmentKeys.Where(k => k.ConfigEnvironmentId == foundEnvironment.Id));

            _context.ConfigEnvironments.Remove(foundEnvironment);

            try
            {
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete environment {identifier} from database: {e}");
                return Result.Error($"could not delete environment {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteStructure(StructureIdentifier identifier)
        {
            var foundStructure = await GetStructureInternal(identifier);

            if (foundStructure is null)
                return Result.Success();

            _context.Structures.Remove(foundStructure);

            try
            {
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete Structure {identifier} from database: {e}");
                return Result.Error($"could not delete Structure {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> GenerateEnvironmentKeyAutocompleteData(EnvironmentIdentifier identifier)
        {
            var environment = await GetEnvironmentInternal(identifier);

            var roots = new List<ConfigEnvironmentKeyPath>();

            foreach (var environmentKey in environment.Keys.OrderBy(k => k.Key))
            {
                var parts = environmentKey.Key.Split('/');

                var rootPart = parts.First();
                var root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new ConfigEnvironmentKeyPath
                    {
                        Id = Guid.NewGuid(),
                        ConfigEnvironment = environment,
                        ConfigEnvironmentId = environment.Id,
                        Children = new List<ConfigEnvironmentKeyPath>(),
                        Parent = null,
                        ParentId = null,
                        Path = rootPart,
                        FullPath = rootPart
                    };

                    roots.Add(root);
                }

                var current = root;

                foreach (var part in parts.Skip(1))
                {
                    var next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

                    if (next is null)
                    {
                        next = new ConfigEnvironmentKeyPath
                        {
                            Id = Guid.NewGuid(),
                            ConfigEnvironment = environment,
                            ConfigEnvironmentId = environment.Id,
                            Children = new List<ConfigEnvironmentKeyPath>(),
                            Parent = current,
                            ParentId = current.Id,
                            Path = part,
                            FullPath = current.FullPath + '/' + part
                        };

                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            var existingPaths = await _context.AutoCompletePaths
                                              .Where(p => p.ConfigEnvironmentId == environment.Id)
                                              .ToListAsync();

            _context.AutoCompletePaths.RemoveRange(existingPaths);

            await _context.AutoCompletePaths.AddRangeAsync(roots);

            try
            {
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not update auto-completion data for environment '{identifier}': {e}");
                return Result.Error($"could not update auto-completion data for environment '{identifier}'", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<EnvironmentSnapshot>> GetDefaultEnvironment(string category)
            => await GetEnvironment(new EnvironmentIdentifier(category, "Default"));

        /// <inheritdoc />
        public async Task<IResult<EnvironmentSnapshot>> GetEnvironment(EnvironmentIdentifier identifier)
        {
            var environment = await GetEnvironmentInternal(identifier);

            if (environment == null)
            {
                _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                return Result.Error<EnvironmentSnapshot>($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
            }

            var environmentData = environment.Keys
                                             .ToDictionary(data => data.Key,
                                                           data => data.Value);

            return Result.Success(new EnvironmentSnapshot(identifier, environmentData));
        }

        /// <inheritdoc />
        public async Task<IResult<EnvironmentSnapshot>> GetEnvironmentWithInheritance(EnvironmentIdentifier identifier)
        {
            var environment = await GetEnvironmentInternal(identifier);

            if (environment is null)
            {
                _logger.LogError($"no {nameof(Environment)} with id {identifier} found");
                return Result.Error<EnvironmentSnapshot>($"no {nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
            }

            var environmentData = environment.Keys
                                             .ToDictionary(data => data.Key,
                                                           data => data.Value);

            var defaultEnv = await GetEnvironmentInternal(new EnvironmentIdentifier(identifier.Category, "Default"));

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

        /// <inheritdoc />
        public async Task<ConfigurationIdentifier> GetLatestActiveConfiguration()
        {
            var metadata = await _context.Metadata.FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            var configId = metadata.LastActiveConfigurationId;
            if (configId == Guid.Empty)
                return null;

            var configuration = await _context.FullProjectedConfigurations
                                              .Where(c => c.Id == configId)
                                              .Select(c => new ConfigurationIdentifier(
                                                          new EnvironmentIdentifier(c.ConfigEnvironment.Category, c.ConfigEnvironment.Name),
                                                          new StructureIdentifier(c.Structure.Name, c.Structure.Version)))
                                              .FirstOrDefaultAsync();

            return configuration;
        }

        /// <inheritdoc />
        public async Task<long?> GetLatestProjectedEventId()
        {
            var metadata = await _context.Metadata.FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            return metadata.LatestEvent;
        }

        /// <inheritdoc />
        public async Task<IResult<StructureSnapshot>> GetStructure(StructureIdentifier identifier)
        {
            var structure = await GetStructureInternal(identifier);

            if (structure == null)
            {
                _logger.LogError($"no {nameof(Structure)} with id {identifier} found");
                return Result.Error<StructureSnapshot>($"no {nameof(Structure)} with id {identifier} found", ErrorCode.NotFound);
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

        /// <inheritdoc />
        public async Task<IResult> ImportEnvironment(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            var environment = await GetEnvironmentInternal(identifier);

            if (environment is null)
            {
                _logger.LogError($"could not find environment {identifier} to apply modifications");
                return Result.Error($"could not find environment {identifier} to apply modifications", ErrorCode.NotFound);
            }

            _context.ConfigEnvironmentKeys.RemoveRange(environment.Keys);
            _context.ConfigEnvironmentKeys.AddRange(actions.Where(a => a.Type == ConfigKeyActionType.Set)
                                                           .Select(a => new ConfigEnvironmentKey
                                                           {
                                                               Key = a.Key,
                                                               Description = a.Description,
                                                               ConfigEnvironment = environment,
                                                               ConfigEnvironmentId = environment.Id,
                                                               Id = Guid.NewGuid(),
                                                               Type = a.ValueType,
                                                               Value = a.Value
                                                           }));

            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<IResult> SaveConfiguration(EnvironmentSnapshot environment,
                                                     StructureSnapshot structure,
                                                     IDictionary<string, string> configuration,
                                                     string configurationJson,
                                                     IEnumerable<string> usedKeys,
                                                     DateTime? validFrom,
                                                     DateTime? validTo)
        {
            var foundEnvironment = await GetEnvironmentInternal(environment.Identifier);

            // version is already included in structure.Identifier, as opposed to environment.Identifier
            var foundStructure = await GetStructureInternal(structure.Identifier);

            var compiledConfiguration = new ProjectedConfiguration
            {
                Id = Guid.NewGuid(),
                ConfigEnvironmentId = foundEnvironment.Id,
                StructureId = foundStructure.Id,
                StructureVersion = foundStructure.Version,
                ConfigurationJson = configurationJson,
                UpToDate = true,
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

            var existingConfiguration = await _context.ProjectedConfigurations
                                                      .FirstOrDefaultAsync(c => c.ConfigEnvironment.Category == environment.Identifier.Category &&
                                                                                c.ConfigEnvironment.Name == environment.Identifier.Name &&
                                                                                c.Structure.Name == structure.Identifier.Name &&
                                                                                c.Structure.Version == structure.Identifier.Version &&
                                                                                c.ValidFrom == validFrom &&
                                                                                c.ValidTo == validTo);

            try
            {
                if (existingConfiguration != null)
                    _context.ProjectedConfigurations.Remove(existingConfiguration);

                _context.ProjectedConfigurations.Add(compiledConfiguration);

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save compiled configuration: {e}");
                return Result.Error($"could not save compiled configuration: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task SetLatestActiveConfiguration(ConfigurationIdentifier identifier)
        {
            var metadata = await _context.Metadata.FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            var configuration = await _context.ProjectedConfigurations
                                              .Where(c => c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                          c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                          c.Structure.Name == identifier.Structure.Name &&
                                                          c.Structure.Version == identifier.Structure.Version)
                                              .Select(c => c.Id)
                                              .FirstOrDefaultAsync();

            metadata.LastActiveConfigurationId = configuration;
        }

        /// <inheritdoc />
        public async Task SetLatestProjectedEventId(long latestEventId)
        {
            var metadata = await _context.Metadata.FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            metadata.LatestEvent = latestEventId;
        }

        private async Task<ConfigEnvironment> GetEnvironmentInternal(EnvironmentIdentifier identifier)
            => await _context.FullConfigEnvironments
                             .FirstOrDefaultAsync(env => env.Category == identifier.Category &&
                                                         env.Name == identifier.Name);

        private async Task<Structure> GetStructureInternal(StructureIdentifier identifier)
            => await _context.FullStructures
                             .FirstOrDefaultAsync(str => str.Name == identifier.Name &&
                                                         str.Version == identifier.Version);
    }
}