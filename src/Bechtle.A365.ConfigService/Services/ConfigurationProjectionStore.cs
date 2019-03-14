using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class ConfigurationProjectionStore : IConfigurationProjectionStore
    {
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<ConfigurationProjectionStore> _logger;

        /// <inheritdoc />
        public ConfigurationProjectionStore(ProjectionStoreContext context, ILogger<ConfigurationProjectionStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when, QueryRange range)
        {
            try
            {
                var utcWhen = when.ToUniversalTime();
                var dbResult = await _context.ProjectedConfigurations
                                             .Include(c => c.ConfigEnvironment)
                                             .Include(c => c.Structure)
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= utcWhen &&
                                                         (c.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ThenBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .ToListAsync();
                var identifiers = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                          .ToList()
                                  ?? new List<ConfigurationIdentifier>();

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ConfigurationIdentifier>>> GetStale(QueryRange range)
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Include(c => c.ConfigEnvironment)
                                             .Include(c => c.Structure)
                                             .Where(p => !p.UpToDate)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ThenBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .ToListAsync();

                var identifiers = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                          .ToList()
                                  ?? new List<ConfigurationIdentifier>();

                return Result.Success<IList<ConfigurationIdentifier>>(identifiers);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
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
                var dbResult = await _context.ProjectedConfigurations
                                             .Include(c => c.ConfigEnvironment)
                                             .Include(c => c.Structure)
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .Where(c => c.ConfigEnvironment.Category == environment.Category &&
                                                         c.ConfigEnvironment.Name == environment.Name)
                                             .OrderBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .ToListAsync();

                var result = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                     .ToList()
                             ?? new List<ConfigurationIdentifier>();

                return Result.Success<IList<ConfigurationIdentifier>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
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
                var dbResult = await _context.ProjectedConfigurations
                                             .Include(c => c.ConfigEnvironment)
                                             .Include(c => c.Structure)
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .Where(c => c.Structure.Name == structure.Name &&
                                                         c.Structure.Version == structure.Version)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .ToListAsync();

                var result = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                     .ToList()
                             ?? new List<ConfigurationIdentifier>();

                return Result.Success<IList<ConfigurationIdentifier>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
                return Result.Error<IList<ConfigurationIdentifier>>("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<JToken>> GetJson(ConfigurationIdentifier identifier, DateTime when)
        {
            var formattedParams = "(" +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Category)}: {identifier.Environment.Category}; " +
                                  $"{nameof(identifier.Environment)}{nameof(identifier.Environment.Name)}: {identifier.Environment.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Name)}: {identifier.Structure.Name}; " +
                                  $"{nameof(identifier.Structure)}{nameof(identifier.Structure.Version)}: {identifier.Structure.Version}" +
                                  ")";

            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .Where(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                         c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                         c.Structure.Name == identifier.Structure.Name &&
                                                         c.Structure.Version == identifier.Structure.Version)
                                             .Select(c => c.ConfigurationJson)
                                             .FirstOrDefaultAsync();

                if (dbResult is null)
                    return Result.Error<JToken>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                try
                {
                    var result = JToken.Parse(dbResult);

                    return Result.Success(result);
                }
                catch (JsonException e)
                {
                    _logger.LogError($"failed to parse projected Configuration-Json into token: {e}");
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configuration keys for id: {formattedParams}: {e}");
                return Result.Error<JToken>($"failed to retrieve projected configuration keys for id: {formattedParams}",
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
                var dbResult = await _context.FullProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                                       c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                                       c.Structure.Name == identifier.Structure.Name &&
                                                                       c.Structure.Version == identifier.Structure.Version);

                if (dbResult is null)
                    return Result.Error<IDictionary<string, string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = dbResult.Keys
                                     .OrderBy(k => k.Key)
                                     .Skip(range.Offset)
                                     .Take(range.Length)
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configuration keys for id: {formattedParams}: {e}");
                return Result.Error<IDictionary<string, string>>($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                                 ErrorCode.DbQueryError);
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
                var dbResult = await _context.FullProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                                       c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                                       c.Structure.Name == identifier.Structure.Name &&
                                                                       c.Structure.Version == identifier.Structure.Version);

                if (dbResult is null)
                    return Result.Error<IEnumerable<string>>($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = dbResult.UsedConfigurationKeys
                                     .OrderBy(k => k.Key)
                                     .Skip(range.Offset)
                                     .Take(range.Length)
                                     .Select(usedKey => usedKey.Key)
                                     .ToArray();

                return Result.Success<IEnumerable<string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve used environment keys for id: {formattedParams}: {e}");
                return Result.Error<IEnumerable<string>>($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                                         ErrorCode.DbQueryError);
            }
        }
    }
}