using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class EnvironmentProjectionStore : IEnvironmentProjectionStore
    {
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<EnvironmentProjectionStore> _logger;

        /// <inheritdoc />
        public EnvironmentProjectionStore(ProjectionStoreContext context, ILogger<EnvironmentProjectionStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                var dbResult = await _context.FullConfigEnvironments
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .Select(s => new EnvironmentIdentifier(s.Category, s.Name))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result.Success<IList<EnvironmentIdentifier>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result.Error<IList<EnvironmentIdentifier>>("failed to retrieve environments", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<EnvironmentIdentifier>>> GetAvailableInCategory(string category, QueryRange range)
        {
            try
            {
                var dbResult = await _context.FullConfigEnvironments
                                             .Where(s => s.Category == category)
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .Select(s => new EnvironmentIdentifier(s.Category, s.Name))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result.Success<IList<EnvironmentIdentifier>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result.Error<IList<EnvironmentIdentifier>>($"failed to retrieve environments in '{category}'", ErrorCode.DbQueryError);
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

                var environmentKey = await _context.ConfigEnvironments
                                                   .Where(s => s.Category == identifier.Category &&
                                                               s.Name == identifier.Name)
                                                   .Select(env => env.Id)
                                                   .FirstOrDefaultAsync();

                if (environmentKey == Guid.Empty)
                    return Result.Error<IList<DtoConfigKeyCompletion>>("no environment found with (" +
                                                                       $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                       $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                       ErrorCode.NotFound);

                // send auto-completion data for all roots
                if (string.IsNullOrWhiteSpace(key))
                    return await CreateResult(await _context.FullAutoCompletePaths
                                                            .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                                        p.ParentId == null)
                                                            .ToListAsync(),
                                              range);

                var parts = new Queue<string>(key.Contains('/')
                                                  ? key.Split('/')
                                                  : new[] {key});

                var rootPart = parts.Dequeue();

                // if the user is searching within the roots
                if (!parts.Any())
                {
                    var possibleRoots = await _context.FullAutoCompletePaths
                                                      .Include(c => c.Children)
                                                      .ThenInclude(c => c.Children)
                                                      .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                                  p.ParentId == null &&
                                                                  p.Path.Contains(rootPart))
                                                      .ToListAsync();

                    return await (possibleRoots.Count == 1 && possibleRoots.First()
                                                                           .Path
                                                                           .Equals(rootPart, StringComparison.OrdinalIgnoreCase)
                                      ? CreateResult(possibleRoots.First().Children, range)
                                      : CreateResult(possibleRoots, range));
                }

                var root = await _context.FullAutoCompletePaths
                                         .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                     p.ParentId == null &&
                                                     p.Path == rootPart)
                                         .FirstOrDefaultAsync();

                if (root is null)
                    return Result.Error<IList<DtoConfigKeyCompletion>>($"key '{key}' is ambiguous, root does not match anything",
                                                                       ErrorCode.NotFound);

                return await GetKeyAutoCompleteInternal(root, parts, range);
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
        public Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier,
                                                                      QueryRange range)
            => GetKeysInternal(identifier,
                               null,
                               null,
                               range,
                               item => new DtoConfigKey
                               {
                                   Key = item.Key,
                                   Value = item.Value,
                                   Description = item.Description,
                                   Type = item.Type
                               },
                               item => item.Key,
                               items => items);

        /// <inheritdoc />
        public Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier,
                                                                      string filter,
                                                                      QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               null,
                               range,
                               item => new DtoConfigKey
                               {
                                   Key = item.Key,
                                   Value = item.Value,
                                   Description = item.Description,
                                   Type = item.Type
                               },
                               item => item.Key,
                               items => items);

        /// <inheritdoc />
        public Task<IResult<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier,
                                                                      string filter,
                                                                      string preferExactMatch,
                                                                      QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               preferExactMatch,
                               range,
                               item => new DtoConfigKey
                               {
                                   Key = item.Key,
                                   Value = item.Value,
                                   Description = item.Description,
                                   Type = item.Type
                               },
                               item => item.Key,
                               items => items);

        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier,
                                                                  string filter,
                                                                  string preferExactMatch,
                                                                  QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               preferExactMatch,
                               range,
                               item => item,
                               item => item.Key,
                               keys => (IDictionary<string, string>) keys.ToImmutableSortedDictionary(item => item.Key,
                                                                                                      item => item.Value,
                                                                                                      StringComparer.OrdinalIgnoreCase));

        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, QueryRange range)
            => GetKeysInternal(identifier,
                               null,
                               null,
                               range,
                               item => item,
                               item => item.Key,
                               keys => (IDictionary<string, string>) keys.ToImmutableSortedDictionary(item => item.Key,
                                                                                                      item => item.Value,
                                                                                                      StringComparer.OrdinalIgnoreCase));

        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier,
                                                                  string filter,
                                                                  QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               null,
                               range,
                               item => item,
                               item => item.Key,
                               keys => (IDictionary<string, string>) keys.ToImmutableSortedDictionary(item => item.Key,
                                                                                                      item => item.Value,
                                                                                                      StringComparer.OrdinalIgnoreCase));

        /// <summary>
        ///     retrieve all direct children for the given paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private async Task CollectChildren(params ConfigEnvironmentKeyPath[] paths)
        {
            foreach (var path in paths)
                path.Children = await _context.AutoCompletePaths
                                              .Include(p => p.Parent)
                                              .Include(p => p.ConfigEnvironment)
                                              .Where(p => p.ParentId == path.Id)
                                              .OrderBy(p => p.Path)
                                              .ToListAsync();
        }

        /// <summary>
        ///     take the given paths - retrieve children if necessary - and return an them as DTOs + Result
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private async Task<IResult<IList<DtoConfigKeyCompletion>>> CreateResult(IEnumerable<ConfigEnvironmentKeyPath> paths, QueryRange range)
        {
            var array = paths.ToArray();
            foreach (var path in array)
                if (path.Children is null)
                    await CollectChildren(path);

            return Result.Success<IList<DtoConfigKeyCompletion>>(array.OrderBy(p => p.Path)
                                                                      .Skip(range.Offset)
                                                                      .Take(range.Length)
                                                                      .Select(p => new DtoConfigKeyCompletion
                                                                      {
                                                                          Completion = p.Path,
                                                                          FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                                                          HasChildren = p.Children.Any()
                                                                      })
                                                                      .ToList());
        }

        /// <summary>
        ///     walk the path given in <paramref name="parts" />, from <paramref name="root" /> and return the next possible values
        /// </summary>
        /// <param name="root"></param>
        /// <param name="parts"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private async Task<IResult<IList<DtoConfigKeyCompletion>>> GetKeyAutoCompleteInternal(ConfigEnvironmentKeyPath root,
                                                                                              IEnumerable<string> parts,
                                                                                              QueryRange range)
        {
            var current = root;
            var result = new List<ConfigEnvironmentKeyPath>();
            var queue = new Queue<string>(parts);

            // try walking the given path to the deepest part, and return all options the user can take from here
            while (queue.TryDequeue(out var part))
            {
                await CollectChildren(current);

                if (string.IsNullOrWhiteSpace(part))
                {
                    result = current.Children;
                    break;
                }

                var match = current.Children.FirstOrDefault(c => c.Path.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (!(match is null))
                {
                    await CollectChildren(match);
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

            return await CreateResult(result, range);
        }

        /// <summary>
        ///     retrieve keys from the database as dictionary, allows for filtering and range-limiting
        /// </summary>
        /// <param name="identifier">ID pointing to an Environment, which is used to retrieve the base-keys</param>
        /// <param name="filter">optional filter that is applied to the start of all keys</param>
        /// <param name="preferExactMatch">optional filter that, when matched, only returns the matching item - otherwise all matching items are returned</param>
        /// <param name="range">offset and amount of items to query</param>
        /// <param name="selector">internal selector transforming the filtered items to an intermediate representation</param>
        /// <param name="keySelector">selector pointing to the 'Key' property of the intermediate representation</param>
        /// <param name="transform">final transformation applied to the result-set</param>
        /// <returns></returns>
        private async Task<IResult<TResult>> GetKeysInternal<TItem, TResult>(EnvironmentIdentifier identifier,
                                                                             string filter,
                                                                             string preferExactMatch,
                                                                             QueryRange range,
                                                                             Expression<Func<ConfigEnvironmentKey, TItem>> selector,
                                                                             Func<TItem, string> keySelector,
                                                                             Func<IEnumerable<TItem>, TResult> transform)
            where TItem : class
        {
            try
            {
                var envId = await _context.FullConfigEnvironments
                                          .Where(s => s.Category == identifier.Category &&
                                                      s.Name == identifier.Name)
                                          .Select(e => e.Id)
                                          .FirstOrDefaultAsync();

                if (envId == default)
                    return Result.Error<TResult>("no environment found with (" +
                                                 $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                 $"{nameof(identifier.Name)}: {identifier.Name})",
                                                 ErrorCode.NotFound);

                var query = _context.ConfigEnvironmentKeys
                                    .Where(k => k.ConfigEnvironmentId == envId);

                if (!string.IsNullOrWhiteSpace(filter))
                    query = query.Where(k => k.Key.StartsWith(filter));

                var keys = await query.OrderBy(k => k.Key)
                                      .Skip(range.Offset)
                                      .Take(range.Length)
                                      .Select(selector)
                                      .ToListAsync();

                if (!string.IsNullOrWhiteSpace(preferExactMatch))
                    keys = ApplyPreferredExactFilter(keys, keySelector, preferExactMatch).ToList();

                var result = transform(keys);

                return Result.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result.Error<TResult>(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
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
    }
}