using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public class LayerProjectionStore : ILayerProjectionStore
    {
        private readonly IDomainObjectManager _domainObjectManager;
        private readonly ILogger<LayerProjectionStore> _logger;

        /// <inheritdoc cref="LayerProjectionStore" />
        public LayerProjectionStore(
            ILogger<LayerProjectionStore> logger,
            IDomainObjectManager domainObjectManager)
        {
            _logger = logger;
            _domainObjectManager = domainObjectManager;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => new(Task.CompletedTask);

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public async Task<IResult> Clone(LayerIdentifier sourceId, LayerIdentifier targetId)
        {
            _logger.LogDebug(
                "cloning layer {SourceLayerId} as {TargetLayerId}",
                sourceId,
                targetId);

            return await _domainObjectManager.CloneLayer(sourceId, targetId, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> Create(LayerIdentifier identifier)
        {
            _logger.LogDebug("attempting to create new Layer {Identifier}", identifier);

            return await _domainObjectManager.CreateLayer(identifier, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> Delete(LayerIdentifier identifier)
        {
            _logger.LogDebug("attempting to delete layer {Identifier}", identifier);

            return await _domainObjectManager.DeleteLayer(identifier, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteKeys(LayerIdentifier identifier, ICollection<string> keysToDelete)
        {
            _logger.LogDebug("attempting to delete '{DeletedKeys}' keys from {Identifier}", keysToDelete.Count, identifier);

            List<ConfigKeyAction> deletions = keysToDelete.Select(ConfigKeyAction.Delete).ToList();

            return await _domainObjectManager.ModifyLayerKeys(identifier, deletions, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<LayerIdentifier>>> GetAvailable(QueryRange range)
        {
            _logger.LogDebug("collecting available layers, range={Range}", range);
            return await _domainObjectManager.GetLayers(range, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<LayerIdentifier>>> GetAvailable(QueryRange range, long version)
        {
            _logger.LogDebug("collecting available layers, range={Range}", range);
            return await _domainObjectManager.GetLayers(range, version, CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<IResult<Page<DtoConfigKeyCompletion>>> GetKeyAutoComplete(
            LayerIdentifier identifier,
            string? key,
            QueryRange range)
            => GetKeyAutoComplete(identifier, key, range, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<Page<DtoConfigKeyCompletion>>> GetKeyAutoComplete(
            LayerIdentifier identifier,
            string? key,
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
                _logger.LogDebug("using new key='{Key}'", key);

                IResult<EnvironmentLayer> layerResult = await _domainObjectManager.GetLayer(identifier, version, CancellationToken.None);
                if (layerResult.IsError)
                {
                    _logger.LogWarning(
                        "unable to retrieve layer {Identifier}: {ErrorCode}, {Message}",
                        identifier,
                        layerResult.Code,
                        layerResult.Message);

                    return Result.Error<Page<DtoConfigKeyCompletion>>(layerResult.Message, layerResult.Code);
                }

                EnvironmentLayer layer = layerResult.CheckedData;
                List<EnvironmentLayerKeyPath> paths = layer.KeyPaths;

                // send auto-completion data for all roots
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogDebug("early-exit, sending root-paths because key was empty");
                    List<DtoConfigKeyCompletion> rootItems = paths.Select(
                                                                      p => new DtoConfigKeyCompletion
                                                                      {
                                                                          HasChildren = p.Children.Any(),
                                                                          FullPath = p.FullPath,
                                                                          Completion = p.Path
                                                                      })
                                                                  .OrderBy(p => p.Completion)
                                                                  .ToList();

                    return Result.Success(
                        new Page<DtoConfigKeyCompletion>
                        {
                            Items = rootItems,
                            Count = rootItems.Count,
                            Offset = 0,
                            TotalCount = rootItems.Count
                        });
                }

                var parts = new Queue<string>(
                    key.Contains('/')
                        ? key.Split('/')
                        : new[] { key });

                string rootPart = parts.Dequeue();

                _logger.LogDebug("starting with path '{Root}'", rootPart);

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

                    List<DtoConfigKeyCompletion> totalItems =
                        selectedRoots.Select(
                                         p => new DtoConfigKeyCompletion
                                         {
                                             HasChildren = p.Children.Any(),
                                             FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                             Completion = p.Path
                                         })
                                     .OrderBy(p => p.Completion)
                                     .ToList();

                    List<DtoConfigKeyCompletion> selectedItems = totalItems.Skip(range.Offset)
                                                                           .Take(range.Length)
                                                                           .ToList();

                    return Result.Success(
                        new Page<DtoConfigKeyCompletion>
                        {
                            Items = selectedItems,
                            Count = selectedItems.Count,
                            Offset = range.Offset,
                            TotalCount = totalItems.Count
                        });
                }

                EnvironmentLayerKeyPath? root = paths.FirstOrDefault(p => p.Path == rootPart);

                if (root is null)
                {
                    _logger.LogDebug("no path found that matches '{Root}'", rootPart);
                    return Result.Error<Page<DtoConfigKeyCompletion>>(
                        $"key '{key}' is ambiguous, root does not match anything",
                        ErrorCode.NotFound);
                }

                IResult<Page<DtoConfigKeyCompletion>> result = GetKeyAutoCompleteInternal(root, parts.ToList(), range);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "failed to get autocomplete data for '{Path}' in {Identifier}",
                    key,
                    identifier);

                return Result.Error<Page<DtoConfigKeyCompletion>>(
                    $"failed to get autocomplete data for '{key}' in {identifier}: {e}",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<DtoConfigKey>>> GetKeyObjects(KeyQueryParameters<LayerIdentifier> parameters)
        {
            _logger.LogDebug("retrieving keys for {Layer} to return as objects", parameters.Identifier);

            IResult<Page<DtoConfigKey>> result = await GetKeysInternal(
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
            {
                return result;
            }

            _logger.LogDebug("got {ItemCount} keys as objects", result.CheckedData.Count);

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
            {
                return RemoveRoot(result.CheckedData, parameters.RemoveRoot);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult<Page<KeyValuePair<string, string?>>>> GetKeys(KeyQueryParameters<LayerIdentifier> parameters)
        {
            _logger.LogDebug("retrieving keys of layer '{Layer}'", parameters.Identifier);

            IResult<Page<KeyValuePair<string, string?>>> result =
                await GetKeysInternal(
                    parameters,
                    item => item,
                    item => item.Key,
                    key => new KeyValuePair<string, string?>(key.Key, key.Value));

            if (result.IsError)
            {
                return result;
            }

            _logger.LogDebug("got {KeyCount} keys for '{Layer}'", result.CheckedData.Count, parameters.Identifier);

            if (!string.IsNullOrWhiteSpace(parameters.RemoveRoot))
            {
                return RemoveRoot(result.CheckedData, parameters.RemoveRoot);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IResult<EnvironmentLayerMetadata>> GetMetadata(LayerIdentifier identifier)
        {
            _logger.LogDebug("retrieving metadata for {Identifier}", identifier);

            IResult<EnvironmentLayer> layerResult = await _domainObjectManager.GetLayer(identifier, CancellationToken.None);

            if (layerResult.IsError)
            {
                return Result.Error<EnvironmentLayerMetadata>(layerResult.Message, layerResult.Code);
            }

            EnvironmentLayer layer = layerResult.CheckedData;

            var metadata = new EnvironmentLayerMetadata(layer.Id)
            {
                AssignedTo = layer.UsedInEnvironments,
                Tags = layer.Tags,
                ChangedAt = layer.ChangedAt,
                ChangedBy = layer.ChangedBy,
                CreatedAt = layer.CreatedAt,
                CreatedBy = layer.CreatedBy,
                KeyCount = layer.Keys.Count,
            };

            return Result.Success(metadata);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<EnvironmentLayerMetadata>>> GetMetadata(QueryRange range, long version)
        {
            _logger.LogDebug("retrieving metadata for range: {Range} at version {Version}", range, version);

            IResult<Page<LayerIdentifier>> ids = await _domainObjectManager.GetLayers(range, version, CancellationToken.None);
            if (ids.IsError)
            {
                return Result.Error<Page<EnvironmentLayerMetadata>>(ids.Message, ids.Code);
            }

            var results = new List<EnvironmentLayerMetadata>();
            foreach (LayerIdentifier layerId in ids.CheckedData.Items)
            {
                IResult<EnvironmentLayerMetadata> result = await GetMetadata(layerId);
                if (result.IsError)
                {
                    return Result.Error<Page<EnvironmentLayerMetadata>>(result.Message, result.Code);
                }

                results.Add(result.CheckedData);
            }

            return Result.Success(
                new Page<EnvironmentLayerMetadata>
                {
                    Items = results,
                    Count = results.Count,
                    Offset = range.Offset,
                    TotalCount = ids.CheckedData.TotalCount
                });
        }

        /// <inheritdoc />
        public async Task<IResult<Page<string>>> GetTags(LayerIdentifier identifier)
        {
            _logger.LogDebug("retrieving tags of layer '{Identifier}'", identifier);

            IResult<EnvironmentLayer> result = await _domainObjectManager.GetLayer(identifier, CancellationToken.None);

            if (result.IsError)
            {
                return Result.Error<Page<string>>(result.Message, result.Code);
            }

            return Result.Success(
                new Page<string>
                {
                    Items = result.CheckedData.Tags,
                    Count = result.CheckedData.Tags.Count,
                    Offset = 0,
                    TotalCount = result.CheckedData.Tags.Count
                });
        }

        /// <inheritdoc />
        public async Task<IResult> UpdateKeys(LayerIdentifier identifier, ICollection<DtoConfigKey> keys)
        {
            _logger.LogDebug("attempting to update {UpdatedKeys} keys in '{Identifier}'", keys.Count, identifier);

            List<ConfigKeyAction> updates = keys.Select(k => ConfigKeyAction.Set(k.Key, k.Value, k.Description, k.Type))
                                                .ToList();

            return await _domainObjectManager.ModifyLayerKeys(identifier, updates, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> UpdateTags(LayerIdentifier identifier, ICollection<string> addedTags, ICollection<string> removedTags)
        {
            _logger.LogDebug(
                "updating tags for layer '{Identifier}', {Additions} new tags, {Deletions} removed tags",
                identifier,
                addedTags.Count,
                removedTags.Count);

            IResult<EnvironmentLayer> layerResult = await _domainObjectManager.GetLayer(identifier, CancellationToken.None);
            if (layerResult.IsError)
            {
                return layerResult;
            }

            var additions = new List<string>();
            var deletions = new List<string>();

            EnvironmentLayer layer = layerResult.CheckedData;

            additions.AddRange(addedTags.Where(t => !layer.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
            deletions.AddRange(removedTags.Where(t => layer.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));

            return await _domainObjectManager.UpdateTags(identifier, additions, deletions, CancellationToken.None);
        }

        private IEnumerable<TItem> ApplyPreferredExactFilter<TItem>(
            IList<TItem> items,
            Func<TItem, string> keySelector,
            string preferredMatch)
            where TItem : class
        {
            _logger.LogDebug(
                "applying PreferredMatch filter to '{ItemCount}' items, using preferredMatch='{PreferredMatch}'",
                items.Count,
                preferredMatch);

            var exactMatch = items.FirstOrDefault(item => keySelector(item).Equals(preferredMatch));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(
                    exactMatch is null
                        ? "no exact match found in '{ItemCount}' items for query '{PreferredMatch}'"
                        : "preferred match found in '{ItemsCount}' items for query '{PreferredMatch}'",
                    items.Count,
                    preferredMatch);

            return exactMatch is null
                       ? items
                       : new[] { exactMatch };
        }

        /// <summary>
        ///     walk the path given in <paramref name="parts" />, from <paramref name="root" /> and return the next possible values
        /// </summary>
        /// <param name="root"></param>
        /// <param name="parts"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private IResult<Page<DtoConfigKeyCompletion>> GetKeyAutoCompleteInternal(
            EnvironmentLayerKeyPath root,
            ICollection<string> parts,
            QueryRange range)
        {
            _logger.LogDebug(
                "walking path from '{Root}' using ({Parts}), range={Range}",
                root.FullPath,
                string.Join(",", parts),
                range);

            EnvironmentLayerKeyPath current = root;
            var result = new List<EnvironmentLayerKeyPath>();
            var queue = new Queue<string>(parts);
            var walkedPath = new List<string> { "{START}" };

            // try walking the given path to the deepest part, and return all options the user can take from here
            while (queue.TryDequeue(out var part))
            {
                _logger.LogTrace("try walking down '{Part}'", part);

                if (string.IsNullOrWhiteSpace(part))
                {
                    _logger.LogDebug("next part is empty, returning '{ItemCount}' children", current.Children.Count);
                    result = current.Children;
                    break;
                }

                EnvironmentLayerKeyPath? match = current.Children.FirstOrDefault(c => c.Path.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (!(match is null))
                {
                    _logger.LogDebug(
                        "match for '{Part}' found, continuing at '{MatchPart}' with '{MatchChildren}' children",
                        part,
                        match.FullPath,
                        match.Children.Count);
                    walkedPath.Add(current.Path);
                    current = match;
                    result = match.Children;
                    continue;
                }

                _logger.LogDebug("no matches found, suggesting ");
                List<EnvironmentLayerKeyPath> suggested = current.Children
                                                                 .Where(c => c.Path.Contains(part, StringComparison.OrdinalIgnoreCase))
                                                                 .ToList();

                if (suggested.Any())
                {
                    result = suggested;
                    break;
                }

                walkedPath.Add("{END}");
                string walkedPathStr = string.Join(" => ", walkedPath);

                string logMsg = $"no matching objects for '{part}' found; " + $"path taken to get to this dead-end: {walkedPathStr}";

                _logger.LogDebug(logMsg);

                return Result.Error<Page<DtoConfigKeyCompletion>>(logMsg, ErrorCode.NotFound);
            }

            List<DtoConfigKeyCompletion> totalItems = result.Select(
                                                                p => new DtoConfigKeyCompletion
                                                                {
                                                                    FullPath = p.FullPath,
                                                                    HasChildren = p.Children.Any(),
                                                                    Completion = p.Path
                                                                })
                                                            .OrderBy(p => p.Completion)
                                                            .ToList();

            List<DtoConfigKeyCompletion> selectedItems = totalItems.Skip(range.Offset)
                                                                   .Take(range.Length)
                                                                   .ToList();

            return Result.Success(
                new Page<DtoConfigKeyCompletion>
                {
                    Items = selectedItems,
                    Count = selectedItems.Count,
                    Offset = range.Offset,
                    TotalCount = totalItems.Count
                });
        }

        /// <summary>
        ///     retrieve keys from the database as dictionary, allows for filtering and range-limiting
        /// </summary>
        /// <param name="parameters">see <see cref="KeyQueryParameters{EnvironmentIdentifier}" /> for more information on each parameter</param>
        /// <param name="selector">internal selector transforming the filtered items to an intermediate representation</param>
        /// <param name="keySelector">selector pointing to the 'Key' property of the intermediate representation</param>
        /// <param name="transform">final transformation applied to the result-set</param>
        /// <returns></returns>
        private async Task<IResult<Page<TResult>>> GetKeysInternal<TItem, TResult>(
            KeyQueryParameters<LayerIdentifier> parameters,
            Expression<Func<EnvironmentLayerKey, TItem>> selector,
            Func<TItem, string> keySelector,
            Func<TItem, TResult> transform)
            where TItem : class
        {
            try
            {
                _logger.LogDebug(
                    "retrieving keys using parameters:"
                    + "Identifier='{Identifier}', Filter='{Filter}', PreferExactMatch='{PreferExactMatch}', "
                    + "Range='{Range}', RemoveRoot='{RemoveRoot}'",
                    parameters.Identifier,
                    parameters.Filter,
                    parameters.PreferExactMatch,
                    parameters.Range,
                    parameters.RemoveRoot);

                IResult<EnvironmentLayer> layerResult = await _domainObjectManager.GetLayer(parameters.Identifier, CancellationToken.None);
                if (layerResult.IsError)
                    return Result.Error<Page<TResult>>(layerResult.Message, layerResult.Code);

                EnvironmentLayer layer = layerResult.CheckedData;

                IQueryable<EnvironmentLayerKey> query = layer.Keys.Values.AsQueryable();

                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                {
                    _logger.LogDebug("adding filter '{Filter}'", parameters.Filter);
                    query = query.Where(k => k.Key.StartsWith(parameters.Filter));
                }

                _logger.LogDebug("ordering, and paging data");
                List<TItem> keys = query.OrderBy(k => k.Key)
                                        .Select(selector)
                                        .ToList();

                if (!string.IsNullOrWhiteSpace(parameters.PreferExactMatch))
                {
                    _logger.LogDebug("applying preferredMatch filter: '{Filter}'", parameters.PreferExactMatch);
                    keys = ApplyPreferredExactFilter(keys, keySelector, parameters.PreferExactMatch).ToList();
                }

                _logger.LogDebug("transforming result using closure");
                List<TResult> result = keys.Skip(parameters.Range.Offset)
                                           .Take(parameters.Range.Length)
                                           .Select(transform)
                                           .ToList();

                var page = new Page<TResult>
                {
                    Items = result,
                    Count = result.Count,
                    Offset = parameters.Range.Offset,
                    TotalCount = keys.Count
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for layer {Identifier}", parameters.Identifier);
                return Result.Error<Page<TResult>>($"failed to retrieve keys for layer ({parameters.Identifier})", ErrorCode.DbQueryError);
            }
        }

        /// <summary>
        ///     remove the 'root' portion of each given key
        /// </summary>
        /// <returns></returns>
        private IResult<Page<KeyValuePair<string, string?>>> RemoveRoot(Page<KeyValuePair<string, string?>> page, string root)
        {
            try
            {
                _logger.LogDebug("attempting to remove root '{Root}' from '{ItemCount}' items", root, page.Items.Count);

                if (!root.EndsWith('/'))
                {
                    _logger.LogDebug("appending '/' to root, to make results uniform");
                    root += '/';
                }

                // if every item passes the check for the same root
                // project each item into a new dict with the modified Key
                if (page.Items.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug("all keys start with given root '{Root}', re-rooting possible", root);
                    page.Items = page.Items
                                     .Select(kvp => new KeyValuePair<string, string?>(kvp.Key[root.Length..], kvp.Value))
                                     .ToList();
                    return Result.Success(page);
                }

                _logger.LogDebug("could not remove root '{Root}' from all entries - not all items share same root", root);
                return Result.Error<Page<KeyValuePair<string, string?>>>(
                    $"could not remove root '{root}' from all entries - not all items share same root",
                    ErrorCode.InvalidData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "could not remove root '{Root}' from all entries", root);
                return Result.Error<Page<KeyValuePair<string, string?>>>($"could not remove root '{root}' from all entries", ErrorCode.Undefined);
            }
        }

        /// <summary>
        ///     remove the 'root' portion of each given key
        /// </summary>
        /// <returns></returns>
        private IResult<Page<DtoConfigKey>> RemoveRoot(Page<DtoConfigKey> page, string root)
        {
            try
            {
                if (!root.EndsWith('/'))
                {
                    _logger.LogDebug("appending '/' to root, to make results uniform");
                    root += '/';
                }

                // if every item passes the check for the same root
                // modify the .Key property and put the entries into a new list that we return
                if (page.Items.All(k => k.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug("all keys start with given root '{Root}', re-rooting possible", root);
                    page.Items = page.Items.Select(
                                         entry =>
                                         {
                                             entry.Key = entry.Key[root.Length..];
                                             return entry;
                                         })
                                     .ToList();
                    return Result.Success(page);
                }

                _logger.LogDebug("could not remove root '{Root}' from all ConfigKeys - not all items share same root", root);
                return Result.Error<Page<DtoConfigKey>>(
                    $"could not remove root '{root}' from all ConfigKeys - not all items share same root",
                    ErrorCode.InvalidData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "could not remove root '{Root}' from all ConfigKeys", root);
                return Result.Error<Page<DtoConfigKey>>($"could not remove root '{root}' from all ConfigKeys", ErrorCode.Undefined);
            }
        }
    }
}
