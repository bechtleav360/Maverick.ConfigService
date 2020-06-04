using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public sealed class EnvironmentProjectionStore : IEnvironmentProjectionStore
    {
        private readonly IEventStore _eventStore;
        private readonly ILogger<EnvironmentProjectionStore> _logger;
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IList<ICommandValidator> _validators;

        /// <inheritdoc cref="EnvironmentProjectionStore" />
        public EnvironmentProjectionStore(IEventStore eventStore,
                                          IDomainObjectStore domainObjectStore,
                                          ILogger<EnvironmentProjectionStore> logger,
                                          IEnumerable<ICommandValidator> validators)
        {
            _logger = logger;
            _validators = validators.ToList();
            _eventStore = eventStore;
            _domainObjectStore = domainObjectStore;
        }

        /// <inheritdoc />
        public async Task<IResult> Create(EnvironmentIdentifier identifier, bool isDefault)
        {
            _logger.LogDebug($"attempting to create new {(isDefault ? "Default" : "")}Environment '{identifier}'");

            var envResult = await _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString());
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            _logger.LogDebug("creating environment");
            var createResult = environment.Create(isDefault);
            if (createResult.IsError)
                return createResult;

            _logger.LogDebug("validating resulting events");
            var errors = environment.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await environment.WriteRecordedEvents(_eventStore);
        }

        /// <inheritdoc />
        public async Task<IResult> Delete(EnvironmentIdentifier identifier)
        {
            _logger.LogDebug($"attempting to delete environment '{identifier}'");

            var envResult = await _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString());
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            _logger.LogDebug("deleting environment");
            var createResult = environment.Delete();
            if (createResult.IsError)
                return createResult;

            _logger.LogDebug("validating resulting events");
            var errors = environment.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await environment.WriteRecordedEvents(_eventStore);
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteKeys(EnvironmentIdentifier identifier, ICollection<string> keysToDelete)
        {
            _logger.LogDebug($"attempting to delete '{keysToDelete.Count}' keys from '{identifier}'");

            var envResult = await _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString());
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            _logger.LogDebug($"deleting '{keysToDelete.Count}' keys from environment '{identifier}'");
            var result = environment.DeleteKeys(keysToDelete);
            if (result.IsError)
                return result;

            _logger.LogDebug("validating resulting events");
            var errors = environment.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await environment.WriteRecordedEvents(_eventStore);
        }

        /// <inheritdoc />
        public Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range)
            => GetAvailable(range, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range, long version)
        {
            try
            {
                _logger.LogDebug($"collecting available environments, range={range}");

                var envList = await (version <= 0
                                         ? _domainObjectStore.ReplayObject<ConfigEnvironmentList>()
                                         : _domainObjectStore.ReplayObject<ConfigEnvironmentList>(version));

                List<EnvironmentIdentifier> identifiers;

                if (envList.IsError)
                {
                    identifiers = new List<EnvironmentIdentifier>();
                    _logger.LogInformation($"could not build EnvironmentList to collect environments: {envList.Code}; {envList.Message}");
                }
                else
                {
                    identifiers = envList.Data
                                         .GetIdentifiers()
                                         .OrderBy(e => e.Category)
                                         .ThenBy(e => e.Name)
                                         .Skip(range.Offset)
                                         .Take(range.Length)
                                         .ToList();

                    _logger.LogDebug($"collected '{identifiers.Count}' identifiers");
                }

                return Result.Success<IList<EnvironmentIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result.Error<IList<EnvironmentIdentifier>>("failed to retrieve environments", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier,
                                                                               string key,
                                                                               QueryRange range)
            => GetKeyAutoComplete(identifier, key, range, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier,
                                                                                     string key,
                                                                                     QueryRange range,
                                                                                     long version)
        {
            try
            {
                _logger.LogDebug($"attempting to retrieve next paths in '{identifier}' for path '{key}', range={range}");

                _logger.LogDebug($"removing escape-sequences from key");
                key = Uri.UnescapeDataString(key ?? string.Empty);
                _logger.LogDebug($"using new key='{key}'");

                var envResult = await (version <= 0
                                           ? _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString())
                                           : _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString(), version));

                if (envResult.IsError)
                    return Result.Error<IList<DtoConfigKeyCompletion>>(
                        "no environment found with (" +
                        $"{nameof(identifier.Category)}: {identifier.Category}; " +
                        $"{nameof(identifier.Name)}: {identifier.Name})",
                        ErrorCode.NotFound);

                var environment = envResult.Data;
                var paths = environment.KeyPaths;

                // send auto-completion data for all roots
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogDebug("early-exit, sending root-paths because key was empty");
                    return Result.Success<IList<DtoConfigKeyCompletion>>(
                        paths.Select(p => new DtoConfigKeyCompletion
                             {
                                 HasChildren = p.Children.Any(),
                                 FullPath = p.FullPath,
                                 Completion = p.Path
                             })
                             .OrderBy(p => p.Completion)
                             .ToList());
                }

                var parts = new Queue<string>(key.Contains('/')
                                                  ? key.Split('/')
                                                  : new[] {key});

                var rootPart = parts.Dequeue();

                _logger.LogDebug($"starting with path '{rootPart}'");

                // if the user is searching within the roots
                if (!parts.Any())
                {
                    _logger.LogDebug($"no further parts found, returning direct children");

                    var possibleRoots = paths.Where(p => p.Path.Contains(rootPart))
                                             .ToList();

                    // if there is only one possible root, and that one matches what were searching for
                    // we're returning all paths directly below that one
                    List<ConfigEnvironmentKeyPath> selectedRoots;
                    if (possibleRoots.Count == 1
                        && possibleRoots.First()
                                        .Path
                                        .Equals(rootPart, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("selecting children of only root for further processing");
                        selectedRoots = paths.First().Children;
                    }
                    else
                    {
                        _logger.LogDebug("selecting original matches for further processing");
                        selectedRoots = paths;
                    }

                    return Result.Success<IList<DtoConfigKeyCompletion>>(
                        selectedRoots.Select(p => new DtoConfigKeyCompletion
                                     {
                                         HasChildren = p.Children.Any(),
                                         FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                         Completion = p.Path
                                     })
                                     .OrderBy(p => p.Completion)
                                     .ToList());
                }

                var root = paths.FirstOrDefault(p => p.Path == rootPart);

                if (root is null)
                {
                    _logger.LogDebug($"no path found that matches '{rootPart}'");
                    return Result.Error<IList<DtoConfigKeyCompletion>>(
                        $"key '{key}' is ambiguous, root does not match anything",
                        ErrorCode.NotFound);
                }

                var result = GetKeyAutoCompleteInternal(root, parts.ToList(), range);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to get autocomplete data for '{key}' in " +
                                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})");

                return Result.Error<IList<DtoConfigKeyCompletion>>($"failed to get autocomplete data for '{key}' in " +
                                                                   $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}",
                                                                   ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentKeyQueryParameters parameters)
        {
            _logger.LogDebug($"retrieving keys for {parameters.Environment} to return as objects");

            var result = await GetKeysInternal(parameters,
                                               item => new DtoConfigKey
                                               {
                                                   Key = item.Key,
                                                   Value = item.Value,
                                                   Description = item.Description,
                                                   Type = item.Type
                                               },
                                               item => item.Key,
                                               items => items);

            if (result.IsError)
                return result;

            var list = result.Data.ToList();

            _logger.LogDebug($"got {list.Count} keys as objects");

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
                return RemoveRoot(list, parameters.RemoveRoot);

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentKeyQueryParameters parameters)
        {
            _logger.LogDebug($"retrieving keys of environment '{parameters.Environment}'");

            var result = await GetKeysInternal(parameters,
                                               item => item,
                                               item => item.Key,
                                               keys => (IDictionary<string, string>) keys.ToImmutableDictionary(item => item.Key,
                                                                                                                item => item.Value,
                                                                                                                StringComparer.OrdinalIgnoreCase));

            if (result.IsError)
                return result;

            _logger.LogDebug($"got {result.Data.Count} keys for '{parameters.Environment}'");

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
                return RemoveRoot(result.Data, parameters.RemoveRoot);

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult> UpdateKeys(EnvironmentIdentifier identifier, ICollection<DtoConfigKey> keys)
        {
            _logger.LogDebug($"attempting to update {keys.Count} keys in '{identifier}'");

            var envResult = await _domainObjectStore.ReplayObject(new ConfigEnvironment(identifier), identifier.ToString());
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            if (!environment.Created)
                return Result.Error("environment does not exist", ErrorCode.NotFound);

            _logger.LogDebug($"transforming DTOs to '{nameof(ConfigEnvironmentKey)}'");
            var updates = keys.Select(dto => new ConfigEnvironmentKey(dto.Key, dto.Value, dto.Type, dto.Description, 0))
                              .ToList();

            _logger.LogDebug("updating environment keys");
            var result = environment.UpdateKeys(updates);

            if (result.IsError)
                return result;

            _logger.LogDebug("validating generated events");
            var errors = environment.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await environment.WriteRecordedEvents(_eventStore);
        }

        private IEnumerable<TItem> ApplyPreferredExactFilter<TItem>(IList<TItem> items,
                                                                    Func<TItem, string> keySelector,
                                                                    string preferredMatch)
            where TItem : class
        {
            _logger.LogDebug($"applying PreferredMatch filter to '{items.Count}' items, using {nameof(preferredMatch)}='{preferredMatch}'");

            var exactMatch = items.FirstOrDefault(item => keySelector(item).Equals(preferredMatch));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(exactMatch is null
                                     ? $"no exact match found in '{items.Count}' items for query '{preferredMatch}'"
                                     : $"preferred match found in '{items.Count}' items for query '{preferredMatch}'");

            return exactMatch is null
                       ? items
                       : new[] {exactMatch};
        }

        /// <summary>
        ///     walk the path given in <paramref name="parts" />, from <paramref name="root" /> and return the next possible values
        /// </summary>
        /// <param name="root"></param>
        /// <param name="parts"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private IResult<IList<DtoConfigKeyCompletion>> GetKeyAutoCompleteInternal(ConfigEnvironmentKeyPath root,
                                                                                  ICollection<string> parts,
                                                                                  QueryRange range)
        {
            _logger.LogDebug($"walking path from '{root.FullPath}' using ({string.Join(",", parts)}), range={range}");

            var current = root;
            var result = new List<ConfigEnvironmentKeyPath>();
            var queue = new Queue<string>(parts);

            // try walking the given path to the deepest part, and return all options the user can take from here
            while (queue.TryDequeue(out var part))
            {
                _logger.LogTrace($"try walking down '{part}'");

                if (string.IsNullOrWhiteSpace(part))
                {
                    _logger.LogDebug($"next part is empty, returning '{current.Children.Count}' children");
                    result = current.Children;
                    break;
                }

                var match = current.Children.FirstOrDefault(c => c.Path.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (!(match is null))
                {
                    _logger.LogDebug($"match for '{part}' found, continuing at '{match.FullPath}' with '{match.Children.Count}' children");
                    current = match;
                    result = match.Children;
                    continue;
                }

                _logger.LogDebug($"no matches found, suggesting ");
                var suggested = current.Children
                                       .Where(c => c.Path.Contains(part, StringComparison.OrdinalIgnoreCase))
                                       .ToList();

                if (suggested.Any())
                {
                    result = suggested;
                    break;
                }

                // take the current ConfigEnvironmentPath object and walk its parents back to the root
                // to gather info for a more descriptive error-message
                var walkedPath = new List<string>();
                var x = current;

                walkedPath.Add($"{{END; '{part}'}}");

                while (!(x is null))
                {
                    walkedPath.Add(x.Path);
                    x = x.Parent;
                }

                walkedPath.Add("{START}");
                walkedPath.Reverse();

                var walkedPathStr = string.Join(" => ", walkedPath);

                var logMsg = $"no matching objects for '{part}' found; " +
                             $"path taken to get to this dead-end: {walkedPathStr}";

                _logger.LogDebug(logMsg);

                return Result.Error<IList<DtoConfigKeyCompletion>>(logMsg, ErrorCode.NotFound);
            }

            return Result.Success<IList<DtoConfigKeyCompletion>>(
                result.Select(p => new DtoConfigKeyCompletion
                      {
                          FullPath = p.FullPath,
                          HasChildren = p.Children.Any(),
                          Completion = p.Path
                      })
                      .OrderBy(p => p.Completion)
                      .Skip(range.Offset)
                      .Take(range.Length)
                      .ToList());
        }

        /// <summary>
        ///     retrieve keys from the database as dictionary, allows for filtering and range-limiting
        /// </summary>
        /// <param name="parameters">see <see cref="EnvironmentKeyQueryParameters" /> for more information on each parameter</param>
        /// <param name="selector">internal selector transforming the filtered items to an intermediate representation</param>
        /// <param name="keySelector">selector pointing to the 'Key' property of the intermediate representation</param>
        /// <param name="transform">final transformation applied to the result-set</param>
        /// <returns></returns>
        private async Task<IResult<TResult>> GetKeysInternal<TItem, TResult>(EnvironmentKeyQueryParameters parameters,
                                                                             Expression<Func<ConfigEnvironmentKey, TItem>> selector,
                                                                             Func<TItem, string> keySelector,
                                                                             Func<IEnumerable<TItem>, TResult> transform)
            where TItem : class
        {
            try
            {
                _logger.LogDebug("retrieving keys using parameters:" +
                                 $"{nameof(parameters.Environment)}: {parameters.Environment}, " +
                                 $"{nameof(parameters.Filter)}: {parameters.Filter}, " +
                                 $"{nameof(parameters.PreferExactMatch)}: {parameters.PreferExactMatch}, " +
                                 $"{nameof(parameters.Range)}: {parameters.Range}, " +
                                 $"{nameof(parameters.RemoveRoot)}: {parameters.RemoveRoot}");

                // if TargetVersion is above 0, we try to find the specified version of ConfigEnvironment
                var envResult = await (parameters.TargetVersion <= 0
                                           ? _domainObjectStore.ReplayObject(new ConfigEnvironment(parameters.Environment),
                                                                             parameters.Environment.ToString())
                                           : _domainObjectStore.ReplayObject(new ConfigEnvironment(parameters.Environment),
                                                                             parameters.Environment.ToString(),
                                                                             parameters.TargetVersion));
                if (envResult.IsError)
                    return Result.Error<TResult>(envResult.Message, envResult.Code);

                var environment = envResult.Data;

                if (!environment.Created)
                    return Result.Error<TResult>($"environment '{environment.Identifier}' does not exist", ErrorCode.NotFound);

                var query = environment.Keys
                                       .Values
                                       .AsQueryable();

                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                {
                    _logger.LogDebug($"adding filter '{parameters.Filter}'");
                    query = query.Where(k => k.Key.StartsWith(parameters.Filter));
                }

                _logger.LogDebug("ordering, and paging data");
                var keys = query.OrderBy(k => k.Key)
                                .Skip(parameters.Range.Offset)
                                .Take(parameters.Range.Length)
                                .Select(selector)
                                .ToList();

                if (!string.IsNullOrWhiteSpace(parameters.PreferExactMatch))
                {
                    _logger.LogDebug($"applying preferredMatch filter: '{parameters.PreferExactMatch}'");
                    keys = ApplyPreferredExactFilter(keys, keySelector, parameters.PreferExactMatch).ToList();
                }

                _logger.LogDebug("transforming result using closure");
                var result = transform(keys);

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve keys for environment ({parameters.Environment})");
                return Result.Error<TResult>($"failed to retrieve keys for environment ({parameters.Environment})", ErrorCode.DbQueryError);
            }
        }

        /// <summary>
        ///     remove the 'root' portion of each given key
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        private IResult<IDictionary<string, string>> RemoveRoot(IDictionary<string, string> keys, string root)
        {
            try
            {
                _logger.LogDebug($"attempting to remove root '{root}' from '{keys.Count}' items");

                if (!root.EndsWith('/'))
                {
                    _logger.LogDebug("appending '/' to root, to make results uniform");
                    root += '/';
                }

                // if every item passes the check for the same root
                // project each item into a new dict with the modified Key
                if (keys.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug($"all keys start with given root '{root}', re-rooting possible");
                    return Result.Success((IDictionary<string, string>) keys.ToDictionary(kvp => kvp.Key.Substring(root.Length),
                                                                                          kvp => kvp.Value,
                                                                                          StringComparer.OrdinalIgnoreCase));
                }

                _logger.LogDebug($"could not remove root '{root}' from all entries - not all items share same root");
                return Result.Error<IDictionary<string, string>>($"could not remove root '{root}' from all entries - not all items share same root",
                                                                 ErrorCode.InvalidData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"could not remove root '{root}' from all entries");
                return Result.Error<IDictionary<string, string>>($"could not remove root '{root}' from all entries", ErrorCode.Undefined);
            }
        }

        /// <summary>
        ///     remove the 'root' portion of each given key
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        private IResult<IEnumerable<DtoConfigKey>> RemoveRoot(IEnumerable<DtoConfigKey> keys, string root)
        {
            try
            {
                if (!root.EndsWith('/'))
                {
                    _logger.LogDebug("appending '/' to root, to make results uniform");
                    root += '/';
                }

                var keyList = keys.ToList();

                // if every item passes the check for the same root
                // modify the .Key property and put the entries into a new list that we return
                if (keyList.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug($"all keys start with given root '{root}', re-rooting possible");
                    return Result.Success(keyList.Select(entry =>
                                                 {
                                                     entry.Key = entry.Key.Substring(root.Length);
                                                     return entry;
                                                 })
                                                 .ToList()
                                                 .AsEnumerable());
                }

                _logger.LogDebug($"could not remove root '{root}' from all ConfigKeys - not all items share same root");
                return Result.Error<IEnumerable<DtoConfigKey>>($"could not remove root '{root}' from all ConfigKeys - not all items share same root",
                                                               ErrorCode.InvalidData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"could not remove root '{root}' from all ConfigKeys");
                return Result.Error<IEnumerable<DtoConfigKey>>($"could not remove root '{root}' from all ConfigKeys", ErrorCode.Undefined);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _domainObjectStore?.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_domainObjectStore != null)
                await _domainObjectStore.DisposeAsync();
        }
    }
}