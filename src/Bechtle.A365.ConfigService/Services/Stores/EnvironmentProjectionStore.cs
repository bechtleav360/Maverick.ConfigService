using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class EnvironmentProjectionStore : IEnvironmentProjectionStore
    {
        private readonly IEventStore _eventStore;
        private readonly IStreamedStore _streamedStore;
        private readonly ILogger<EnvironmentProjectionStore> _logger;
        private readonly IList<ICommandValidator> _validators;

        /// <inheritdoc />
        public EnvironmentProjectionStore(IEventStore eventStore,
                                          IStreamedStore streamedStore,
                                          ILogger<EnvironmentProjectionStore> logger,
                                          IEnumerable<ICommandValidator> validators)
        {
            _logger = logger;
            _validators = validators.ToList();
            _eventStore = eventStore;
            _streamedStore = streamedStore;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                var result = await _streamedStore.GetEnvironmentList();

                return Result.Success<IList<EnvironmentIdentifier>>(
                    result.IsError
                        ? new List<EnvironmentIdentifier>()
                        : result.Data.GetIdentifiers().ToList());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result.Error<IList<EnvironmentIdentifier>>("failed to retrieve environments", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier,
                                                                                     string key,
                                                                                     QueryRange range)
        {
            try
            {
                key = Uri.UnescapeDataString(key ?? string.Empty);
                var envResult = await _streamedStore.GetEnvironment(identifier);
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
                    return Result.Success<IList<DtoConfigKeyCompletion>>(
                        paths.Select(p => new DtoConfigKeyCompletion
                             {
                                 HasChildren = p.Children.Any(),
                                 FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                 Completion = p.Path
                             })
                             .OrderBy(p => p.Completion)
                             .ToList());

                var parts = new Queue<string>(key.Contains('/')
                                                  ? key.Split('/')
                                                  : new[] {key});

                var rootPart = parts.Dequeue();

                // if the user is searching within the roots
                if (!parts.Any())
                {
                    var possibleRoots = paths.Where(p => p.Path.Contains(rootPart))
                                             .ToList();

                    // if there is only one possible root, and that one is matches what were searching for
                    // we're returning all paths directly below that one
                    var selectedRoots = possibleRoots.Count == 1
                                        && possibleRoots.First()
                                                        .Path
                                                        .Equals(rootPart, StringComparison.OrdinalIgnoreCase)
                                            ? paths.First().Children
                                            : paths;

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
                    return Result.Error<IList<DtoConfigKeyCompletion>>(
                        $"key '{key}' is ambiguous, root does not match anything",
                        ErrorCode.NotFound);

                var result = GetKeyAutoCompleteInternal(root, parts, range);

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

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
                return RemoveRoot(result.Data.ToList(), parameters.RemoveRoot);

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentKeyQueryParameters parameters)
        {
            var result = await GetKeysInternal(parameters,
                                               item => item,
                                               item => item.Key,
                                               keys => (IDictionary<string, string>) keys.ToImmutableDictionary(item => item.Key,
                                                                                                                item => item.Value,
                                                                                                                StringComparer.OrdinalIgnoreCase));

            if (result.IsError)
                return result;

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
                return RemoveRoot(result.Data, parameters.RemoveRoot);

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult> Create(EnvironmentIdentifier identifier, bool isDefault)
        {
            var envResult = await _streamedStore.GetEnvironment(identifier);
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            var createResult = environment.Create(isDefault);
            if (createResult.IsError)
                return createResult;

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
            var envResult = await _streamedStore.GetEnvironment(identifier);
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            var createResult = environment.Delete();
            if (createResult.IsError)
                return createResult;

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
            var envResult = await _streamedStore.GetEnvironment(identifier);
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            var result = environment.DeleteKeys(keysToDelete);
            if (result.IsError)
                return result;

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
        public async Task<IResult> UpdateKeys(EnvironmentIdentifier identifier, ICollection<DtoConfigKey> keys)
        {
            var envResult = await _streamedStore.GetEnvironment(identifier);
            if (envResult.IsError)
                return envResult;

            var environment = envResult.Data;

            var result = environment.UpdateKeys(keys.Select(dto => new StreamedEnvironmentKey
            {
                Description = dto.Description,
                Type = dto.Type,
                Version = 0,
                Key = dto.Key,
                Value = dto.Value
            }).ToList());

            if (result.IsError)
                return result;

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
        private IResult<IList<DtoConfigKeyCompletion>> GetKeyAutoCompleteInternal(StreamedEnvironmentKeyPath root,
                                                                                  IEnumerable<string> parts,
                                                                                  QueryRange range)
        {
            var current = root;
            var result = new List<StreamedEnvironmentKeyPath>();
            var queue = new Queue<string>(parts);

            // try walking the given path to the deepest part, and return all options the user can take from here
            while (queue.TryDequeue(out var part))
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    result = current.Children;
                    break;
                }

                var match = current.Children.FirstOrDefault(c => c.Path.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (!(match is null))
                {
                    current = match;
                    result = match.Children;
                    continue;
                }

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
                                                                             Expression<Func<StreamedEnvironmentKey, TItem>> selector,
                                                                             Func<TItem, string> keySelector,
                                                                             Func<IEnumerable<TItem>, TResult> transform)
            where TItem : class
        {
            try
            {
                var envResult = await _streamedStore.GetEnvironment(parameters.Environment);
                if (envResult.IsError)
                    return Result.Error<TResult>(envResult.Message, envResult.Code);

                var environment = envResult.Data;

                var query = environment.Keys
                                       .Values
                                       .AsQueryable();

                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                    query = query.Where(k => k.Key.StartsWith(parameters.Filter));

                var keys = query.OrderBy(k => k.Key)
                                .Skip(parameters.Range.Offset)
                                .Take(parameters.Range.Length)
                                .Select(selector)
                                .ToList();

                if (!string.IsNullOrWhiteSpace(parameters.PreferExactMatch))
                    keys = ApplyPreferredExactFilter(keys, keySelector, parameters.PreferExactMatch).ToList();

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
                // if every item passes the check for the same root
                // project each item into a new dict with the modified Key
                if (keys.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                    return Result.Success((IDictionary<string, string>) keys.ToDictionary(kvp => kvp.Key.Substring(root.Length),
                                                                                          kvp => kvp.Value));

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
                var keyList = keys.ToList();

                // if every item passes the check for the same root
                // modify the .Key property and put the entries into a new list that we return
                if (keyList.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                    return Result.Success(keyList.Select(entry =>
                                                 {
                                                     entry.Key = entry.Key.Substring(root.Length);
                                                     return entry;
                                                 })
                                                 .ToList()
                                                 .AsEnumerable());

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
    }
}