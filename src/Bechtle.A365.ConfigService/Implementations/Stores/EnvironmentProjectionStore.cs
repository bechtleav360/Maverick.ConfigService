﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.Common.Utilities.Extensions;
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
        private readonly IDomainObjectManager _domainObjectManager;
        private readonly ILogger<EnvironmentProjectionStore> _logger;

        /// <inheritdoc cref="EnvironmentProjectionStore" />
        public EnvironmentProjectionStore(
            ILogger<EnvironmentProjectionStore> logger,
            IDomainObjectManager domainObjectManager)
        {
            _logger = logger;
            _domainObjectManager = domainObjectManager;
        }

        /// <inheritdoc />
        public async Task<IResult> AssignLayers(EnvironmentIdentifier identifier, IList<LayerIdentifier> layers)
        {
            _logger.LogDebug("assigning {LayerCount} to {Identifier}", layers.Count, identifier);

            return await _domainObjectManager.AssignEnvironmentLayers(identifier, layers, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> Create(EnvironmentIdentifier identifier, bool isDefault)
        {
            _logger.LogDebug("attempting to create new Environment {Identifier}; Default={IsDefault}", identifier, isDefault);

            return await _domainObjectManager.CreateEnvironment(identifier, isDefault, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> Delete(EnvironmentIdentifier identifier)
        {
            _logger.LogDebug("attempting to delete environment {Identifier}", identifier);

            return await _domainObjectManager.DeleteEnvironment(identifier, CancellationToken.None);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
        }

        /// <inheritdoc />
        public Task<IResult<IList<LayerIdentifier>>> GetAssignedLayers(EnvironmentIdentifier identifier) => GetAssignedLayers(identifier, int.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<IList<LayerIdentifier>>> GetAssignedLayers(EnvironmentIdentifier identifier, long version)
        {
            _logger.LogDebug("retrieving layers of environment {Identifier}", identifier);

            IResult<ConfigEnvironment> environmentResult = await _domainObjectManager.GetEnvironment(identifier, CancellationToken.None);
            if (environmentResult.IsError)
            {
                _logger.LogWarning(
                    "unable to load environment {Identifier}: {ErrorCode} {Message}",
                    identifier,
                    environmentResult.Code,
                    environmentResult.Message);

                return Result.Error<IList<LayerIdentifier>>(environmentResult.Message, environmentResult.Code);
            }

            ConfigEnvironment environment = environmentResult.Data;
            IList<LayerIdentifier> layers = environment.Layers.OrderBy(l => l.Name).ToList();

            return Result.Success(layers);
        }

        /// <inheritdoc />
        public Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range)
            => GetAvailable(range, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range, long version)
        {
            _logger.LogDebug("collecting available environments, range={Range}", range);
            return await _domainObjectManager.GetEnvironments(range, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(
            EnvironmentIdentifier identifier,
            string key,
            QueryRange range)
            => GetKeyAutoComplete(identifier, key, range, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(
            EnvironmentIdentifier identifier,
            string key,
            QueryRange range,
            long version)
        {
            try
            {
                _logger.LogDebug(
                    "attempting to retrieve next paths in {Identifier} for path '{Path}', range={Range}",
                    identifier,
                    key,
                    range);

                _logger.LogDebug("removing escape-sequences from key");
                key = Uri.UnescapeDataString(key ?? string.Empty);
                _logger.LogDebug($"using new key='{key}'");

                var environmentResult = await _domainObjectManager.GetEnvironment(identifier, CancellationToken.None);
                if (environmentResult.IsError)
                {
                    _logger.LogWarning(
                        "unable to retrieve environment {Identifier}: {ErrorCode}, {Message}",
                        identifier,
                        environmentResult.Code,
                        environmentResult.Message);

                    return Result.Error<IList<DtoConfigKeyCompletion>>(environmentResult.Message, environmentResult.Code);
                }

                ConfigEnvironment environment = environmentResult.Data;
                List<EnvironmentLayerKeyPath> paths = environment.KeyPaths;

                // send auto-completion data for all roots
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogDebug("early-exit, sending root-paths because key was empty");
                    return Result.Success<IList<DtoConfigKeyCompletion>>(
                        paths.Select(
                                 p => new DtoConfigKeyCompletion
                                 {
                                     HasChildren = p.Children.Any(),
                                     FullPath = p.FullPath,
                                     Completion = p.Path
                                 })
                             .OrderBy(p => p.Completion)
                             .ToList());
                }

                var parts = new Queue<string>(
                    key.Contains('/')
                        ? key.Split('/')
                        : new[] {key});

                string rootPart = parts.Dequeue();

                _logger.LogDebug($"starting with path '{rootPart}'");

                // if the user is searching within the roots
                if (!parts.Any())
                {
                    _logger.LogDebug("no further parts found, returning direct children");

                    List<EnvironmentLayerKeyPath> possibleRoots = paths.Where(p => p.Path.Contains(rootPart))
                                                                       .ToList();

                    // if there is only one possible root, and that one matches what were searching for
                    // we're returning all paths directly below that one
                    List<EnvironmentLayerKeyPath> selectedRoots;
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
                        selectedRoots.Select(
                                         p => new DtoConfigKeyCompletion
                                         {
                                             HasChildren = p.Children.Any(),
                                             FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                             Completion = p.Path
                                         })
                                     .OrderBy(p => p.Completion)
                                     .ToList());
                }

                EnvironmentLayerKeyPath root = paths.FirstOrDefault(p => p.Path == rootPart);

                if (root is null)
                {
                    _logger.LogDebug($"no path found that matches '{rootPart}'");
                    return Result.Error<IList<DtoConfigKeyCompletion>>(
                        $"key '{key}' is ambiguous, root does not match anything",
                        ErrorCode.NotFound);
                }

                IResult<IList<DtoConfigKeyCompletion>> result = GetKeyAutoCompleteInternal(root, parts.ToList(), range);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "failed to get autocomplete data for '{Path}' in {Identifier}",
                    key,
                    identifier);

                return Result.Error<IList<DtoConfigKeyCompletion>>(
                    $"failed to get autocomplete data for '{key}' in {identifier}: {e}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(KeyQueryParameters<EnvironmentIdentifier> parameters)
        {
            _logger.LogDebug($"retrieving keys for {parameters.Identifier} to return as objects");

            var result = await GetKeysInternal(
                             parameters,
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
        public async Task<IResult<IDictionary<string, string>>> GetKeys(KeyQueryParameters<EnvironmentIdentifier> parameters)
        {
            _logger.LogDebug($"retrieving keys of environment '{parameters.Identifier}'");

            var result = await GetKeysInternal(
                             parameters,
                             item => item,
                             item => item.Key,
                             keys => (IDictionary<string, string>) keys.ToImmutableDictionary(
                                 item => item.Key,
                                 item => item.Value,
                                 StringComparer.OrdinalIgnoreCase));

            if (result.IsError)
                return result;

            _logger.LogDebug($"got {result.Data.Count} keys for '{parameters.Identifier}'");

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
                return RemoveRoot(result.Data, parameters.RemoveRoot);

            return result;
        }

        private IEnumerable<TItem> ApplyPreferredExactFilter<TItem>(
            IList<TItem> items,
            Func<TItem, string> keySelector,
            string preferredMatch)
            where TItem : class
        {
            _logger.LogDebug($"applying PreferredMatch filter to '{items.Count}' items, using {nameof(preferredMatch)}='{preferredMatch}'");

            var exactMatch = items.FirstOrDefault(item => keySelector(item).Equals(preferredMatch));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(
                    exactMatch is null
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
        private IResult<IList<DtoConfigKeyCompletion>> GetKeyAutoCompleteInternal(
            EnvironmentLayerKeyPath root,
            ICollection<string> parts,
            QueryRange range)
        {
            _logger.LogDebug($"walking path from '{root.FullPath}' using ({string.Join(",", parts)}), range={range}");

            var current = root;
            var result = new List<EnvironmentLayerKeyPath>();
            var queue = new Queue<string>(parts);
            var walkedPath = new List<string>{"{START}"};

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
                    walkedPath.Add(current.Path);
                    current = match;
                    result = match.Children;
                    continue;
                }

                _logger.LogDebug("no matches found, suggesting ");
                var suggested = current.Children
                                       .Where(c => c.Path.Contains(part, StringComparison.OrdinalIgnoreCase))
                                       .ToList();

                if (suggested.Any())
                {
                    result = suggested;
                    break;
                }

                walkedPath.Add("{END}");
                var walkedPathStr = string.Join(" => ", walkedPath);

                var logMsg = $"no matching objects for '{part}' found; " + $"path taken to get to this dead-end: {walkedPathStr}";

                _logger.LogDebug(logMsg);

                return Result.Error<IList<DtoConfigKeyCompletion>>(logMsg, ErrorCode.NotFound);
            }

            return Result.Success<IList<DtoConfigKeyCompletion>>(
                result.Select(
                          p => new DtoConfigKeyCompletion
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
        /// <param name="parameters">see <see cref="KeyQueryParameters{EnvironmentIdentifier}" /> for more information on each parameter</param>
        /// <param name="selector">internal selector transforming the filtered items to an intermediate representation</param>
        /// <param name="keySelector">selector pointing to the 'Key' property of the intermediate representation</param>
        /// <param name="transform">final transformation applied to the result-set</param>
        /// <returns></returns>
        private async Task<IResult<TResult>> GetKeysInternal<TItem, TResult>(
            KeyQueryParameters<EnvironmentIdentifier> parameters,
            Expression<Func<EnvironmentLayerKey, TItem>> selector,
            Func<TItem, string> keySelector,
            Func<IEnumerable<TItem>, TResult> transform)
            where TItem : class
        {
            try
            {
                _logger.LogDebug(
                    "retrieving keys using parameters:"
                    + $"{nameof(parameters.Identifier)}: {parameters.Identifier}, "
                    + $"{nameof(parameters.Filter)}: {parameters.Filter}, "
                    + $"{nameof(parameters.PreferExactMatch)}: {parameters.PreferExactMatch}, "
                    + $"{nameof(parameters.Range)}: {parameters.Range}, "
                    + $"{nameof(parameters.RemoveRoot)}: {parameters.RemoveRoot}");

                IResult<ConfigEnvironment> envResult = await _domainObjectManager.GetEnvironment(parameters.Identifier, CancellationToken.None);
                if (envResult.IsError)
                    return Result.Error<TResult>(envResult.Message, envResult.Code);

                ConfigEnvironment environment = envResult.Data;

                IQueryable<EnvironmentLayerKey> query = environment.Keys.Values.AsQueryable();

                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                {
                    _logger.LogDebug("adding filter '{Filter}'", parameters.Filter);
                    query = query.Where(k => k.Key.StartsWith(parameters.Filter));
                }

                _logger.LogDebug("ordering, and paging data");
                List<TItem> keys = query.OrderBy(k => k.Key)
                                        .Skip(parameters.Range.Offset)
                                        .Take(parameters.Range.Length)
                                        .Select(selector)
                                        .ToList();

                if (!string.IsNullOrWhiteSpace(parameters.PreferExactMatch))
                {
                    _logger.LogDebug("applying preferredMatch filter: '{Filter}'", parameters.PreferExactMatch);
                    keys = ApplyPreferredExactFilter(keys, keySelector, parameters.PreferExactMatch).ToList();
                }

                _logger.LogDebug("transforming result using closure");
                TResult result = transform(keys);

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for environment {Identifier}", parameters.Identifier);
                return Result.Error<TResult>($"failed to retrieve keys for environment ({parameters.Identifier})", ErrorCode.DbQueryError);
            }
        }

        /// <summary>
        ///     remove the 'root' portion of each given key
        /// </summary>
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
                    return Result.Success(
                        (IDictionary<string, string>) keys.ToDictionary(
                            kvp => kvp.Key.Substring(root.Length),
                            kvp => kvp.Value,
                            StringComparer.OrdinalIgnoreCase));
                }

                _logger.LogDebug($"could not remove root '{root}' from all entries - not all items share same root");
                return Result.Error<IDictionary<string, string>>(
                    $"could not remove root '{root}' from all entries - not all items share same root",
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
                    return Result.Success(
                        keyList.Select(
                                   entry =>
                                   {
                                       entry.Key = entry.Key.Substring(root.Length);
                                       return entry;
                                   })
                               .ToList()
                               .AsEnumerable());
                }

                _logger.LogDebug($"could not remove root '{root}' from all ConfigKeys - not all items share same root");
                return Result.Error<IEnumerable<DtoConfigKey>>(
                    $"could not remove root '{root}' from all ConfigKeys - not all items share same root",
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
