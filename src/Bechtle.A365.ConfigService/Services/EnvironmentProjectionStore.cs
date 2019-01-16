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
        public async Task<Result<IList<string>>> GetKeyAutoComplete(EnvironmentIdentifier identifier, string key, QueryRange range)
        {
            try
            {
                var environmentKey = await _context.ConfigEnvironments
                                                   .Where(s => s.Category == identifier.Category &&
                                                               s.Name == identifier.Name)
                                                   .Select(env => env.Id)
                                                   .FirstOrDefaultAsync();

                if (environmentKey == Guid.Empty)
                    return Result<IList<string>>.Error("no environment found with (" +
                                                       $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                       $"{nameof(identifier.Name)}: {identifier.Name})",
                                                       ErrorCode.NotFound);

                key = Uri.UnescapeDataString(key);

                var parts = key.Split('/');
                IEnumerable<string> paths;

                if (parts.Length == 1)
                {
                    var lastPart = parts.Last();

                    paths = await _context.AutoCompletePaths
                                          .Where(p => p.Path.Contains(lastPart))
                                          .OrderBy(p => p.Path)
                                          .Select(p => p.Path)
                                          .ToListAsync();
                }
                else
                {
                    var lastPart = parts.Last();
                    var fullPathMatch = string.Join('/', parts.Take(parts.Length - 1));

                    paths = await _context.AutoCompletePaths
                                          .Where(p => p.Path.Contains(lastPart))
                                          .Where(p => p.FullPath.StartsWith(fullPathMatch))
                                          .OrderBy(p => p.Path)
                                          .Select(p => p.Path)
                                          .ToListAsync();
                }

                return Result.Success((IList<string>) paths.GroupBy(p => p)
                                                           .Select(p => p.Key)
                                                           .Skip(range.Offset)
                                                           .Take(range.Length)
                                                           .ToList());
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to get autocomplete data for '{key}' in " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IList<string>>.Error($"failed to get autocomplete data for '{key}' in " +
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