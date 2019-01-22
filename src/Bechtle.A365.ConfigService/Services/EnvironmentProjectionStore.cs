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

namespace Bechtle.A365.ConfigService.Services
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
        public async Task<Result<IList<EnvironmentIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .Select(s => new EnvironmentIdentifier(s.Category, s.Name))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result<IList<EnvironmentIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve environments: {e}");
                return Result<IList<EnvironmentIdentifier>>.Error("failed to retrieve environments", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<EnvironmentIdentifier>>> GetAvailableInCategory(string category, QueryRange range)
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .Where(s => s.Category == category)
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .Select(s => new EnvironmentIdentifier(s.Category, s.Name))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result<IList<EnvironmentIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve environments: {e}");
                return Result<IList<EnvironmentIdentifier>>.Error($"failed to retrieve environments in '{category}'", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<DtoConfigKeyCompletion>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range)
        {
            Result<IList<DtoConfigKeyCompletion>> CreateResult(IEnumerable<ConfigEnvironmentKeyPath> paths)
                => Result.Success((IList<DtoConfigKeyCompletion>) paths.OrderBy(p => p.Path)
                                                                       .Skip(range.Offset)
                                                                       .Take(range.Length)
                                                                       .Select(p => new DtoConfigKeyCompletion
                                                                       {
                                                                           Completion = p.Path,
                                                                           FullPath = p.Children.Any() ? p.FullPath + '/' : p.FullPath,
                                                                           HasChildren = p.Children.Any()
                                                                       })
                                                                       .ToList());

            try
            {
                key = Uri.UnescapeDataString(key ?? string.Empty);

                var environmentKey = await _context.ConfigEnvironments
                                                   .Where(s => s.Category == identifier.Category &&
                                                               s.Name == identifier.Name)
                                                   .Select(env => env.Id)
                                                   .FirstOrDefaultAsync();

                if (environmentKey == Guid.Empty)
                    return Result<IList<DtoConfigKeyCompletion>>.Error("no environment found with (" +
                                                                       $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                       $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                       ErrorCode.NotFound);

                // send auto-completion data for all roots
                if (string.IsNullOrWhiteSpace(key))
                    return CreateResult(await _context.AutoCompletePaths
                                                      .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                                  p.ParentId == null)
                                                      .ToListAsync());

                var parts = new Queue<string>(key.Contains('/')
                                                  ? key.Split('/')
                                                  : new[] {key});

                var rootPart = parts.Dequeue();

                // if the user is searching within the roots
                if (!parts.Any())
                {
                    var possibleRoots = await _context.AutoCompletePaths
                                                      .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                                  p.ParentId == null &&
                                                                  p.Path.Contains(rootPart))
                                                      .ToListAsync();

                    if (possibleRoots.Count == 1 && possibleRoots.First().Path.Equals(rootPart, StringComparison.OrdinalIgnoreCase))
                        return CreateResult(possibleRoots.First().Children);

                    return CreateResult(possibleRoots);
                }

                var root = await _context.AutoCompletePaths
                                         .Where(p => p.ConfigEnvironmentId == environmentKey &&
                                                     p.ParentId == null &&
                                                     p.Path == rootPart)
                                         .FirstOrDefaultAsync();

                if (root is null)
                    return Result<IList<DtoConfigKeyCompletion>>.Error($"key '{key}' is ambiguous, root does not match anything",
                                                                       ErrorCode.NotFound);

                var current = root;
                var result = new List<ConfigEnvironmentKeyPath>();

                // try walking the given path to the deepest part, and return all options the user can take from here
                while (parts.TryDequeue(out var part))
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

                    var logMsg = $"can't auto-complete '{key}', next path to walk would be '{part}' but no matching objects; " +
                                 $"path taken to get to this dead-end: {walkedPathStr}";

                    _logger.LogDebug(logMsg);

                    return Result<IList<DtoConfigKeyCompletion>>.Error(logMsg, ErrorCode.NotFound);
                }

                return CreateResult(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to get autocomplete data for '{key}' in " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IList<DtoConfigKeyCompletion>>.Error($"failed to get autocomplete data for '{key}' in " +
                                                                   $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}",
                                                                   ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier, QueryRange range)
            => GetKeysInternal(identifier,
                               null,
                               range,
                               item => new DtoConfigKey
                               {
                                   Key = item.Key,
                                   Value = item.Value,
                                   Description = item.Description,
                                   Type = item.Type
                               },
                               items => items);

        /// <inheritdoc />
        public Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier, string filter, QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               range,
                               item => new DtoConfigKey
                               {
                                   Key = item.Key,
                                   Value = item.Value,
                                   Description = item.Description,
                                   Type = item.Type
                               },
                               items => items);

        /// <inheritdoc />
        public Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, QueryRange range)
            => GetKeysInternal(identifier,
                               null,
                               range,
                               item => item,
                               keys => (IDictionary<string, string>) keys.ToImmutableSortedDictionary(item => item.Key,
                                                                                                      item => item.Value,
                                                                                                      StringComparer.OrdinalIgnoreCase));

        /// <inheritdoc />
        public Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, string filter, QueryRange range)
            => GetKeysInternal(identifier,
                               filter,
                               range,
                               item => item,
                               keys => (IDictionary<string, string>) keys.ToImmutableSortedDictionary(item => item.Key,
                                                                                                      item => item.Value,
                                                                                                      StringComparer.OrdinalIgnoreCase));

        /// <summary>
        ///     retrieve keys from the database as dictionary, allows for filtering and range-limiting
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filter"></param>
        /// <param name="range"></param>
        /// <param name="selector"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        private async Task<Result<TResult>> GetKeysInternal<TItem, TResult>(EnvironmentIdentifier identifier,
                                                                            string filter,
                                                                            QueryRange range,
                                                                            Expression<Func<ConfigEnvironmentKey, TItem>> selector,
                                                                            Func<IEnumerable<TItem>, TResult> transform)
        {
            try
            {
                var envId = await _context.ConfigEnvironments
                                          .Where(s => s.Category == identifier.Category &&
                                                      s.Name == identifier.Name)
                                          .Select(e => e.Id)
                                          .FirstOrDefaultAsync();

                if (envId == default(Guid))
                    return Result<TResult>.Error("no environment found with (" +
                                                 $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                 $"{nameof(identifier.Name)}: {identifier.Name})",
                                                 ErrorCode.NotFound);

                var query = _context.ConfigEnvironmentKeys
                                    .Where(k => k.ConfigEnvironmentId == envId);

                if (!string.IsNullOrWhiteSpace(filter))
                    query = query.Where(k => k.ConfigEnvironmentId == envId)
                                 .Where(k => k.Key.StartsWith(filter));

                var keys = await query.OrderBy(k => k.Key)
                                      .Skip(range.Offset)
                                      .Take(range.Length)
                                      .Select(selector)
                                      .ToListAsync();

                return Result<TResult>.Success(transform(keys));
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<TResult>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }
    }
}