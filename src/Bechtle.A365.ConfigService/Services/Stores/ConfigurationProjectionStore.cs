using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
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
    public class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<ConfigurationProjectionStore> _logger;

        /// <inheritdoc />
        public ConfigurationProjectionStore(ProjectionStoreContext context,
                                            ILogger<ConfigurationProjectionStore> logger,
                                            IMemoryCache cache,
                                            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetAvailable),
                                                       when.Ticks,
                                                       range),
                           async entry =>
                           {
                               var utcWhen = when.ToUniversalTime();
                               var identifiers =
                                   await _context.ProjectedConfigurations
                                                 .Include(c => c.ConfigEnvironment)
                                                 .Include(c => c.Structure)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= utcWhen
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                                                 .OrderBy(s => s.ConfigEnvironment.Category)
                                                 .ThenBy(s => s.ConfigEnvironment.Name)
                                                 .ThenBy(s => s.Structure.Name)
                                                 .ThenByDescending(s => s.Structure.Version)
                                                 .Skip(range.Offset)
                                                 .Take(range.Length)
                                                 .Select(s => ConfigurationIdentifier.From(s))
                                                 .ToListAsync();

                               entry.SetDuration(identifiers is null
                                                     ? CacheDuration.None
                                                     : CacheDuration.Medium,
                                                 _configuration,
                                                 _logger);

                               return Result.Success<IList<ConfigurationIdentifier>>(identifiers ?? new List<ConfigurationIdentifier>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment,
                                                                                               DateTime when,
                                                                                               QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetAvailableWithEnvironment),
                                                       environment,
                                                       when.Ticks,
                                                       range),
                           async entry =>
                           {
                               var identifiers =
                                   await _context.ProjectedConfigurations
                                                 .Include(c => c.ConfigEnvironment)
                                                 .Include(c => c.Structure)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .Where(c => c.ConfigEnvironment.Category == environment.Category
                                                             && c.ConfigEnvironment.Name == environment.Name)
                                                 .OrderBy(s => s.Structure.Name)
                                                 .ThenByDescending(s => s.Structure.Version)
                                                 .Skip(range.Offset)
                                                 .Take(range.Length)
                                                 .Select(s => ConfigurationIdentifier.From(s))
                                                 .ToListAsync();

                               entry.SetDuration(identifiers is null ? CacheDuration.None : CacheDuration.Medium,
                                                 _configuration,
                                                 _logger);

                               return Result.Success<IList<ConfigurationIdentifier>>(identifiers ?? new List<ConfigurationIdentifier>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure,
                                                                                             DateTime when,
                                                                                             QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetAvailableWithStructure),
                                                       structure,
                                                       when.Ticks,
                                                       range),
                           async entry =>
                           {
                               var identifiers =
                                   await _context.ProjectedConfigurations
                                                 .Include(c => c.ConfigEnvironment)
                                                 .Include(c => c.Structure)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .Where(c => c.Structure.Name == structure.Name
                                                             && c.Structure.Version == structure.Version)
                                                 .OrderBy(s => s.ConfigEnvironment.Category)
                                                 .ThenBy(s => s.ConfigEnvironment.Name)
                                                 .Skip(range.Offset)
                                                 .Take(range.Length)
                                                 .Select(s => ConfigurationIdentifier.From (s))
                                                 .ToListAsync();

                               entry.SetDuration(identifiers is null ? CacheDuration.None : CacheDuration.Medium,
                                                 _configuration,
                                                 _logger);

                               return Result.Success<IList<ConfigurationIdentifier>>(identifiers ?? new List<ConfigurationIdentifier>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<JsonElement>> GetJson(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetJson),
                                                       identifier,
                                                       when.Ticks),
                           async entry =>
                           {
                               var dbResult =
                                   await _context.ProjectedConfigurations
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .Where(c => c.ConfigEnvironment.Name == identifier.Environment.Name
                                                             && c.ConfigEnvironment.Category == identifier.Environment.Category
                                                             && c.Structure.Name == identifier.Structure.Name
                                                             && c.Structure.Version == identifier.Structure.Version)
                                                 .Select(c => c.ConfigurationJson)
                                                 .FirstOrDefaultAsync();

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<JsonElement>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);
                               }

                               try
                               {
                                   var result = JsonSerializer.Deserialize<JsonElement>(dbResult);

                                   entry.SetDuration(CacheDuration.Medium, _configuration, _logger);

                                   return Result.Success(result);
                               }
                               catch (JsonException e)
                               {
                                   _logger.LogError(e, "failed to parse projected Configuration-Json into token");
                                   throw;
                               }
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve projected configuration keys for id: {formattedParams}");
                return Result.Error<JsonElement>($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                 ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier,
                                                                        DateTime when,
                                                                        QueryRange range)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetKeys),
                                                       identifier,
                                                       when.Ticks,
                                                       range),
                           async entry =>
                           {
                               var dbResult =
                                   await _context.ProjectedConfigurations
                                                 .Include(e => e.ConfigEnvironment)
                                                 .Include(e => e.Structure)
                                                 .Include(e => e.Keys)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name
                                                                           && c.ConfigEnvironment.Category == identifier.Environment.Category
                                                                           && c.Structure.Name == identifier.Structure.Name
                                                                           && c.Structure.Version == identifier.Structure.Version);

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<IDictionary<string, string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);
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
                _logger.LogError(e, $"failed to retrieve projected configuration keys for id: {formattedParams}");
                return Result.Error<IDictionary<string, string>>($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                                 ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetStale(QueryRange range)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetStale),
                                                       range),
                           async entry =>
                           {
                               var identifiers = await _context.ProjectedConfigurations
                                                               .Include(c => c.ConfigEnvironment)
                                                               .Include(c => c.Structure)
                                                               .Where(p => !p.UpToDate)
                                                               .OrderBy(s => s.ConfigEnvironment.Category)
                                                               .ThenBy(s => s.ConfigEnvironment.Name)
                                                               .ThenBy(s => s.Structure.Name)
                                                               .ThenByDescending(s => s.Structure.Version)
                                                               .Skip(range.Offset)
                                                               .Take(range.Length)
                                                               .Select(s => ConfigurationIdentifier.From(s))
                                                               .ToListAsync();

                               entry.SetDuration(identifiers is null ? CacheDuration.None : CacheDuration.Medium,
                                                 _configuration,
                                                 _logger);

                               return Result.Success<IList<ConfigurationIdentifier>>(identifiers ?? new List<ConfigurationIdentifier>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IEnumerable<string>>> GetUsedConfigurationKeys(ConfigurationIdentifier identifier,
                                                                                 DateTime when,
                                                                                 QueryRange range)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetUsedConfigurationKeys),
                                                       identifier,
                                                       when.Ticks,
                                                       range),
                           async entry =>
                           {
                               var dbResult =
                                   await _context.ProjectedConfigurations
                                                 .Include(e => e.ConfigEnvironment)
                                                 .Include(e => e.Structure)
                                                 .Include(e => e.UsedConfigurationKeys)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name
                                                                           && c.ConfigEnvironment.Category == identifier.Environment.Category
                                                                           && c.Structure.Name == identifier.Structure.Name
                                                                           && c.Structure.Version == identifier.Structure.Version);

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<IEnumerable<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);
                               }

                               var result = dbResult.UsedConfigurationKeys
                                                    .OrderBy(k => k.Key)
                                                    .Skip(range.Offset)
                                                    .Take(range.Length)
                                                    .Select(usedKey => usedKey.Key)
                                                    .ToArray();

                               entry.SetDuration(CacheDuration.Medium, _configuration, _logger);

                               return Result.Success<IEnumerable<string>>(result);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
                return Result.Error<IEnumerable<string>>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                                         ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<string>> GetVersion(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(ConfigurationProjectionStore),
                                                       nameof(GetVersion),
                                                       identifier,
                                                       when.Ticks),
                           async entry =>
                           {
                               var dbResult =
                                   await _context.ProjectedConfigurations
                                                 .Include(e => e.ConfigEnvironment)
                                                 .Include(e => e.Structure)
                                                 .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when
                                                             && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                                 .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name
                                                                           && c.ConfigEnvironment.Category == identifier.Environment.Category
                                                                           && c.Structure.Name == identifier.Structure.Name
                                                                           && c.Structure.Version == identifier.Structure.Version);

                               if (dbResult is null)
                               {
                                   entry.SetDuration(CacheDuration.None, _configuration, _logger);
                                   return Result.Error<string>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);
                               }

                               var result = dbResult.Version.ToString();

                               entry.SetDuration(CacheDuration.Medium, _configuration, _logger);

                               return Result.Success(result);
                           });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve used environment keys for id: {formattedParams}");
                return Result.Error<string>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                            ErrorCode.DbQueryError);
            }
        }
    }
}