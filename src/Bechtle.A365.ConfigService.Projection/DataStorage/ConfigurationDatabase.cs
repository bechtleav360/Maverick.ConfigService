using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<ConfigurationDatabase> _logger;
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        public ConfigurationDatabase(ILogger<ConfigurationDatabase> logger, ProjectionStoreContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IResult> AppendProjectedEventMetadata(ProjectedEventMetadata metadata)
        {
            try
            {
                if (metadata is null)
                    return Result.Error($"{nameof(ProjectedEventMetadata)} is null", ErrorCode.InvalidData);

                await _context.ProjectedEventMetadata.AddAsync(metadata);

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not add event-metadata to list of projected-event metadata");
                return Result.Error("could not add event-metadata to db", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> ApplyChanges(EnvironmentIdentifier identifier, IList<ConfigKeyAction> actions)
        {
            _logger.LogInformation($"applying '{actions.Count}' changes to environment '{identifier}'");

            var environment = await GetEnvironmentInternal(identifier);

            if (environment is null)
            {
                _logger.LogError($"could not find environment {identifier} to apply modifications");
                return Result.Error($"could not find environment {identifier} to apply modifications", ErrorCode.NotFound);
            }

            // 'look-up table' string=>ConfigEnvironmentKey to prevent searching the entire list for each and every key
            var lookup = environment.Keys.ToImmutableDictionary(k => k.Key, k => k);

            // changes we want to make later on
            // many small changes to EF-List environment.Keys will result in abysmal performance
            var addedKeys = new ConcurrentBag<ConfigEnvironmentKey>();
            var removedKeys = new ConcurrentBag<ConfigEnvironmentKey>();
            var changedKeys = new ConcurrentBag<ConfigEnvironmentKey>();

            actions.AsParallel()
                   .Select(action => HandleAction(action, identifier, environment, lookup))
                   .ForAll(tuple =>
                   {
                       if (!(tuple.Added is null))
                           addedKeys.Add(tuple.Added);

                       if (!(tuple.Changed is null))
                           changedKeys.Add(tuple.Changed);

                       if (!(tuple.Removed is null))
                           removedKeys.Add(tuple.Removed);
                   });

            // mark configurations as changed when:
            // UsedKeys contains one of the Changed / Deleted Keys

            foreach (var builtConfig in _context.ProjectedConfigurations
                                                .Include(e => e.ConfigEnvironment)
                                                .Include(e => e.Structure)
                                                .Where(c => c.UpToDate)
                                                .ToArray())
            {
                var usedKeys = await _context.UsedConfigurationKeys
                                             .Where(k => k.ProjectedConfigurationId == builtConfig.Id)
                                             .OrderBy(k => k.Key)
                                             .ToListAsync();

                // if any of the Changed- or Removed-Keys is found in the Keys used to build this Configuration - mark it as stale
                if (changedKeys.Select(ck => ck.Key)
                               .Any(ck => usedKeys.Select(uk => uk.Key)
                                                  .Contains(ck)) ||
                    removedKeys.Select(ck => ck.Key)
                               .Any(ck => usedKeys.Select(uk => uk.Key)
                                                  .Contains(ck)))
                {
                    var envId = new EnvironmentIdentifier(builtConfig.ConfigEnvironment);
                    var structId = new StructureIdentifier(builtConfig.Structure);

                    _logger.LogInformation($"marking Configuration for Environment {envId} and Structure {structId} as Stale");
                    builtConfig.UpToDate = false;
                }
            }

            try
            {
                if (removedKeys.Any())
                {
                    _logger.LogInformation($"removing '{removedKeys.Count}' entries from environment {identifier}");
                    environment.Keys.RemoveRange(removedKeys);
                }

                if (addedKeys.Any())
                {
                    _logger.LogInformation($"adding '{addedKeys.Count}' entries to environment {identifier}");
                    environment.Keys.AddRange(addedKeys);
                }

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not apply actions to environment {identifier}");
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
            var addedKeys = new ConcurrentBag<StructureVariable>();
            var removedKeys = new ConcurrentBag<StructureVariable>();

            actions.AsParallel()
                   .Select(action => HandleAction(action, identifier, structure, keyDict))
                   .ForAll(tuple =>
                   {
                       if (!(tuple.Added is null))
                           addedKeys.Add(tuple.Added);

                       if (!(tuple.Removed is null))
                           removedKeys.Add(tuple.Removed);
                   });

            try
            {
                if (removedKeys.Any())
                    structure.Variables.RemoveRange(removedKeys);

                if (addedKeys.Any())
                    structure.Variables.AddRange(addedKeys);

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not apply actions to structure {identifier}");
                return Result.Error($"could not apply actions to structure {identifier}: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<IResult> Connect() => Task.FromResult(Result.Success());

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
                                                  .Count(env => env.Category.Equals(identifier.Category, StringComparison.OrdinalIgnoreCase)
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

            await _context.SaveChangesAsync();

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
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not save new Structure {identifier} to database");
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
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not delete environment {identifier} from database");
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
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not delete Structure {identifier} from database");
                return Result.Error($"could not delete Structure {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> GenerateEnvironmentKeyAutocompleteData(EnvironmentIdentifier identifier)
        {
            var environment = await GetEnvironmentInternal(identifier);

            if (environment?.Keys is null)
            {
                _logger.LogWarning($"couldn't generate auto-complete data for environment '{identifier}': environment was not found");
                return Result.Error($"couldn't generate auto-complete data for environment '{identifier}': environment was not found", ErrorCode.NotFound);
            }

            var roots = new List<ConfigEnvironmentKeyPath>();

            foreach (var environmentKey in environment.Keys.OrderBy(k => k.Key))
            {
                _logger.LogTrace($"generating autocomplete-data for '{environmentKey.Key}'");

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

                    _logger.LogTrace($"adding root-key '{rootPart}'");

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

                        _logger.LogTrace($"adding child-key to '{current.FullPath}' => '{next.Path}'");

                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            var existingPaths = _context.AutoCompletePaths
                                        .Where(p => p.ConfigEnvironmentId == environment.Id)
                                        .ToList();

            _logger.LogTrace($"removing existing autocomplete-data for environment '{identifier}'");

            _context.AutoCompletePaths.RemoveRange(existingPaths);

            _logger.LogTrace($"adding new autocomplete-data for environment '{identifier}'");

            _context.AutoCompletePaths.AddRange(roots);

            try
            {
                _context.SaveChanges();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, $"could not update auto-completion data for environment '{identifier}'");
                return Result.Error($"could not update auto-completion data for environment '{identifier}'", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<EnvironmentSnapshot>> GetDefaultEnvironment(string category)
        {
            var identifier = new EnvironmentIdentifier(category, "Default");

            var environment = await GetDefaultEnvironmentInternal(identifier);

            if (environment == null)
            {
                _logger.LogError($"no Default-{nameof(Environment)} with id {identifier} found");
                return Result.Error<EnvironmentSnapshot>($"no Default-{nameof(Environment)} with id {identifier} found", ErrorCode.NotFound);
            }

            var environmentData = environment.Keys
                                             .ToDictionary(data => data.Key,
                                                           data => data.Value);

            return Result.Success(new EnvironmentSnapshot(new EnvironmentIdentifier(
                                                              environment.Category,
                                                              environment.Name),
                                                          environmentData));
        }

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

            return Result.Success(new EnvironmentSnapshot(new EnvironmentIdentifier(
                                                              environment.Category,
                                                              environment.Name),
                                                          environmentData));
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

            var defaultEnv = await GetDefaultEnvironmentInternal(new EnvironmentIdentifier(identifier.Category, "Default"));

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
            var metadata = await _context.Metadata
                                         .OrderBy(e => e.Id)
                                         .FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            var configId = metadata.LastActiveConfigurationId;
            if (configId == Guid.Empty)
                return null;

            var configuration = await _context.ProjectedConfigurations
                                              .Where(c => c.Id == configId)
                                              .Select(c => new ConfigurationIdentifier(c))
                                              .FirstOrDefaultAsync();

            return configuration;
        }

        /// <inheritdoc />
        public async Task<long?> GetLatestProjectedEventId()
        {
            var metadata = await _context.Metadata
                                         .OrderBy(e => e.Id)
                                         .FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            return metadata.LatestEvent;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata()
        {
            try
            {
                var items = await _context.ProjectedEventMetadata
                                          .OrderBy(e => e.Index)
                                          .ToListAsync();

                return Result.Success((IList<ProjectedEventMetadata>) items ?? new List<ProjectedEventMetadata>());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "couldn't retrieve list of metadata for projected events");
                return Result.Error<IList<ProjectedEventMetadata>>("could not retrieve list of metadata for projected events",
                                                                   ErrorCode.DbQueryError);
            }
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
                _logger.LogInformation($"environment '{identifier}' does not exist yet, creating it before importing keys");

                var createResult = await CreateEnvironment(identifier, false);
                if (createResult.IsError)
                {
                    _logger.LogError($"could not create environment '{identifier}': {createResult.Code:G} {createResult.Message}");
                    return createResult;
                }

                // save changes to apply added Environment
                await _context.SaveChangesAsync();

                environment = await GetEnvironmentInternal(identifier);

                if (environment is null)
                {
                    _logger.LogError($"environment '{identifier}' still can't be found after creating it, aborting import");
                    return Result.Error($"environment '{identifier}' couldn't be created", ErrorCode.DbUpdateError);
                }
            }

            await ApplyChanges(identifier, environment.Keys.Select(k => ConfigKeyAction.Delete(k.Key)).ToList());
            await ApplyChanges(identifier, actions.Where(a => a.Type == ConfigKeyActionType.Set).ToList());
            await _context.SaveChangesAsync();

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

            var usedKeyList = usedKeys.ToList();

            _logger.LogInformation("determining Configuration.Version through highest Version of used keys");

            if (!foundEnvironment.Keys.Any())
                _logger.LogWarning($"environment '{environment.Identifier}' does not contain any keys - " +
                                   "either the command issued referenced a wrong environment, or the environment has not been setup yet");

            var highestKeyVersion = foundEnvironment.Keys.Any()
                                        ? usedKeyList.Select(k => foundEnvironment.Keys.FirstOrDefault(ek => ek.Key == k))
                                                     .Where(k => !(k is null))
                                                     .Max(k => k.Version)
                                        // fallback to 1 to indicate *something* happened, but nothing correct
                                        : 1;

            _logger.LogInformation($"determined Configuration.Version: {highestKeyVersion}");

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
                // @TODO: investigate if another check is really necessary here - if not, we can reduce the Snapshot parameters to their *Identifier components
                UsedConfigurationKeys = usedKeyList.Where(k => environment.Data.ContainsKey(k))
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
                                    .ToList(),
                Version = highestKeyVersion
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

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "could not save compiled configuration");
                return Result.Error($"could not save compiled configuration: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task SetLatestActiveConfiguration(ConfigurationIdentifier identifier)
        {
            var metadata = await _context.Metadata
                                         .OrderBy(e => e.Id)
                                         .FirstOrDefaultAsync();

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

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SetLatestProjectedEventId(long latestEventId)
        {
            var metadata = await _context.Metadata
                                         .OrderBy(e => e.Id)
                                         .FirstOrDefaultAsync();

            if (metadata is null)
            {
                metadata = new ProjectionMetadata();
                await _context.Metadata.AddAsync(metadata);
            }

            metadata.LatestEvent = latestEventId;

            await _context.SaveChangesAsync();
        }

        private async Task<ConfigEnvironment> GetEnvironmentInternal(EnvironmentIdentifier identifier)
            => await _context.FullConfigEnvironments
                             .FirstOrDefaultAsync(env => env.Category == identifier.Category &&
                                                         env.Name == identifier.Name);

        private async Task<ConfigEnvironment> GetDefaultEnvironmentInternal(EnvironmentIdentifier identifier)
            => await _context.FullConfigEnvironments
                             .SingleOrDefaultAsync(env => env.Category == identifier.Category
                                                          && env.DefaultEnvironment)
               ?? await _context.FullConfigEnvironments
                                .FirstOrDefaultAsync(env => env.Category == identifier.Category
                                                            && env.Name == identifier.Name
                                                            && env.DefaultEnvironment);

        private async Task<Structure> GetStructureInternal(StructureIdentifier identifier)
            => await _context.FullStructures
                             .FirstOrDefaultAsync(str => str.Name == identifier.Name &&
                                                         str.Version == identifier.Version);

        private (StructureVariable Added,
            StructureVariable Changed,
            StructureVariable Removed) HandleAction(ConfigKeyAction action,
                                                    StructureIdentifier identifier,
                                                    Structure structure,
                                                    IReadOnlyDictionary<string, StructureVariable> lookup)
        {
            _logger.LogDebug($"applying '{action.Type:G}' to " +
                             $"Key='{action.Key}'; " +
                             $"ValueType='{action.ValueType}'; " +
                             $"Value='{action.Value}'; " +
                             $"Description='{action.Description}'");

            try
            {
                switch (action.Type)
                {
                    case ConfigKeyActionType.Set:
                    {
                        var (added, changed) = HandleSet(action, structure, lookup);
                        return (added, changed, default);
                    }

                    case ConfigKeyActionType.Delete:
                        return (default, default, HandleDelete(action, identifier, lookup));

                    default:
                        throw new ArgumentOutOfRangeException(nameof(action.Type), action.Type, $"unsupported {nameof(ConfigKeyActionType)}; '{action.Type}'");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error while handling action '{action.Type}'");
            }

            return (default, default, default);
        }

        private (ConfigEnvironmentKey Added,
            ConfigEnvironmentKey Changed,
            ConfigEnvironmentKey Removed) HandleAction(ConfigKeyAction action,
                                                       EnvironmentIdentifier identifier,
                                                       ConfigEnvironment environment,
                                                       IReadOnlyDictionary<string, ConfigEnvironmentKey> lookup)
        {
            _logger.LogDebug($"applying '{action.Type:G}' to " +
                             $"Key='{action.Key}'; " +
                             $"ValueType='{action.ValueType}'; " +
                             $"Value='{action.Value}'; " +
                             $"Description='{action.Description}'");

            try
            {
                switch (action.Type)
                {
                    case ConfigKeyActionType.Set:
                    {
                        var (added, changed) = HandleSet(action, environment, lookup);
                        return (added, changed, default);
                    }

                    case ConfigKeyActionType.Delete:
                        return (default, default, HandleDelete(action, identifier, lookup));

                    default:
                        throw new ArgumentOutOfRangeException(nameof(action.Type), action.Type, $"unsupported {nameof(ConfigKeyActionType)}; '{action.Type}'");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error while handling action '{action.Type}'");
            }

            return (default, default, default);
        }

        private StructureVariable HandleDelete(ConfigKeyAction action,
                                               StructureIdentifier identifier,
                                               IReadOnlyDictionary<string, StructureVariable> lookup)
        {
            var existingKey = lookup.FirstOrDefault(kvp => kvp.Key.Equals(action.Key, StringComparison.InvariantCultureIgnoreCase))
                                    .Value;

            if (!(existingKey is null))
                return existingKey;

            _logger.LogError($"could not remove variable '{action.Key}' from structure {identifier}: not found");
            return default;
        }

        private ConfigEnvironmentKey HandleDelete(ConfigKeyAction action,
                                                  EnvironmentIdentifier identifier,
                                                  IReadOnlyDictionary<string, ConfigEnvironmentKey> lookup)
        {
            var existingKey = lookup.FirstOrDefault(kvp => kvp.Key.Equals(action.Key, StringComparison.InvariantCultureIgnoreCase))
                                    .Value;

            if (!(existingKey is null))
                return existingKey;

            _logger.LogError($"could not remove key '{action.Key}' from environment {identifier}: not found");
            return default;
        }

        private (StructureVariable Added, StructureVariable Changed) HandleSet(ConfigKeyAction action,
                                                                               Structure structure,
                                                                               IReadOnlyDictionary<string, StructureVariable> lookup)
        {
            var existingKey = lookup.FirstOrDefault(kvp => kvp.Key.Equals(action.Key, StringComparison.InvariantCultureIgnoreCase))
                                    .Value;

            if (existingKey is null)
                return (new StructureVariable
                           {
                               // empty guid to indicate EFCore that this is a new entry and should be tracked as Added
                               Id = Guid.Empty,
                               StructureId = structure.Id,
                               Key = action.Key,
                               Value = action.Value
                           }, default);

            existingKey.Value = action.Value;

            return (default, existingKey);
        }

        private (ConfigEnvironmentKey Added, ConfigEnvironmentKey Changed) HandleSet(ConfigKeyAction action,
                                                                                     ConfigEnvironment environment,
                                                                                     IReadOnlyDictionary<string, ConfigEnvironmentKey> lookup)
        {
            var existingKey = lookup.FirstOrDefault(kvp => kvp.Key.Equals(action.Key, StringComparison.InvariantCultureIgnoreCase))
                                    .Value;

            if (existingKey is null)
                return (new ConfigEnvironmentKey
                           {
                               // empty guid to indicate EFCore that this is a new entry and should be tracked as Added
                               Id = Guid.Empty,
                               ConfigEnvironmentId = environment.Id,
                               Key = action.Key,
                               Value = action.Value,
                               Description = action.Description,
                               Type = action.ValueType,
                               Version = (long) DateTime.UtcNow
                                                        .Subtract(_unixEpoch)
                                                        .TotalSeconds
                           }, default);

            existingKey.Value = action.Value;
            existingKey.Description = action.Description;
            existingKey.Type = action.ValueType;
            existingKey.Version = (long) DateTime.UtcNow
                                                 .Subtract(_unixEpoch)
                                                 .TotalSeconds;

            return (default, existingKey);
        }
    }
}