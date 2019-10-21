using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class StructureProjectionStore : IStructureProjectionStore
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<StructureProjectionStore> _logger;

        /// <inheritdoc />
        public StructureProjectionStore(ProjectionStoreContext context,
                                        ILogger<StructureProjectionStore> logger,
                                        IMemoryCache cache,
                                        IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<StructureIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(StructureProjectionStore),
                                                       nameof(GetAvailable),
                                                       range),
                           async entry =>
                           {
                               var dbResult = await _context.Structures
                                                            .OrderBy(s => s.Name)
                                                            .ThenByDescending(s => s.Version)
                                                            .Skip(range.Offset)
                                                            .Take(range.Length)
                                                            .Select(s => new StructureIdentifier(s.Name, s.Version))
                                                            .ToListAsync();

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>());
                               }

                               entry.SetDuration(CacheDuration.Medium, _configuration, _logger);

                               return Result.Success<IList<StructureIdentifier>>(dbResult);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<IList<StructureIdentifier>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<int>>> GetAvailableVersions(string name, QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(StructureProjectionStore),
                                                       nameof(GetAvailableVersions),
                                                       name,
                                                       range),
                           async entry =>
                           {
                               var versions = await _context.Structures
                                                            .Where(s => s.Name == name)
                                                            .OrderBy(s => s.Name)
                                                            .ThenByDescending(s => s.Version)
                                                            .Skip(range.Offset)
                                                            .Take(range.Length)
                                                            .Select(s => s.Version)
                                                            .ToListAsync();

                               if (versions is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Success<IList<int>>(new List<int>());
                               }

                               entry.SetDuration(CacheDuration.Medium, _configuration, _logger);
                               return Result.Success<IList<int>>(versions);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<IList<int>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(
                               nameof(StructureProjectionStore),
                               nameof(GetKeys),
                               identifier,
                               range),
                           async entry =>
                           {
                               var dbResult = await _context.FullStructures
                                                            .FirstOrDefaultAsync(s => s.Name == identifier.Name
                                                                                      && s.Version == identifier.Version);

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                                    $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                                    $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                                    ")",
                                                                                    ErrorCode.NotFound);
                               }

                               var result = dbResult.Keys
                                                    .OrderBy(k => k.Key)
                                                    .Skip(range.Offset)
                                                    .Take(range.Length)
                                                    .ToImmutableSortedDictionary(k => k.Key,
                                                                                 k => k.Value,
                                                                                 StringComparer.OrdinalIgnoreCase);

                               entry.SetDuration(CacheDuration.Medium, _configuration, _logger);
                               return Result.Success<IDictionary<string, string>>(result);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for structure " +
                                 $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version}): {e}");

                return Result.Error<IDictionary<string, string>>(
                    "failed to retrieve keys for structure " +
                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetVariables(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(StructureProjectionStore),
                                                       nameof(GetVariables),
                                                       identifier,
                                                       range),
                           async entry =>
                           {
                               var dbResult = await _context.FullStructures
                                                            .FirstOrDefaultAsync(s => s.Name == identifier.Name
                                                                                      && s.Version == identifier.Version);

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                                    $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                                    $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                                    ")",
                                                                                    ErrorCode.NotFound);
                               }

                               var result = dbResult.Variables
                                                    .OrderBy(v => v.Key)
                                                    .Skip(range.Offset)
                                                    .Take(range.Length)
                                                    .ToImmutableSortedDictionary(k => k.Key,
                                                                                 k => k.Value,
                                                                                 StringComparer.OrdinalIgnoreCase);

                               entry.SetDuration(CacheDuration.Short, _configuration, _logger);
                               return Result.Success<IDictionary<string, string>>(result);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve variables for structure " +
                                 $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version}): {e}");

                return Result.Error<IDictionary<string, string>>(
                    "failed to retrieve variables for structure " +
                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})",
                    ErrorCode.DbQueryError);
            }
        }
    }
}