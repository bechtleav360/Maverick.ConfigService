using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
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
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailable(DateTime when)
        {
            try
            {
                var utcWhen = when.ToUniversalTime();
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= utcWhen &&
                                                         (c.ValidTo ?? DateTime.MaxValue) >= utcWhen)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ThenBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .Select(s => new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier(s.ConfigEnvironment.Category,
                                                                                   s.ConfigEnvironment.Name),
                                                         new StructureIdentifier(s.Structure.Name,
                                                                                 s.Structure.Version)))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<ConfigurationIdentifier>();

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment, DateTime when)
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .Where(c => c.ConfigEnvironment.Category == environment.Category &&
                                                         c.ConfigEnvironment.Name == environment.Name)
                                             .OrderBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .ToListAsync();

                var result = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                     .ToList()
                             ?? new List<ConfigurationIdentifier>();

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure, DateTime when)
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => (c.ValidFrom ?? DateTime.MinValue) <= when && (c.ValidTo ?? DateTime.MaxValue) >= when)
                                             .Where(c => c.Structure.Name == structure.Name &&
                                                         c.Structure.Version == structure.Version)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ToListAsync();

                var result = dbResult?.Select(s => new ConfigurationIdentifier(s))
                                     .ToList()
                             ?? new List<ConfigurationIdentifier>();

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configurations: {e}");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier, DateTime when)
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
                                             .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                                       c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                                       c.Structure.Name == identifier.Structure.Name &&
                                                                       c.Structure.Version == identifier.Structure.Version);

                if (dbResult is null)
                    return Result<IDictionary<string, string>>.Error($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = dbResult.Keys
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                return Result<IDictionary<string, string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve projected configuration keys for id: {formattedParams}: {e}");
                return Result<IDictionary<string, string>>.Error($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                                 ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<string>>> GetUsedConfigurationKeys(ConfigurationIdentifier identifier, DateTime when)
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
                                             .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                                       c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                                       c.Structure.Name == identifier.Structure.Name &&
                                                                       c.Structure.Version == identifier.Structure.Version);

                if (dbResult is null)
                    return Result<IEnumerable<string>>.Error($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                var result = dbResult.UsedConfigurationKeys
                                     .Select(usedKey => usedKey.Key)
                                     .OrderBy(_ => _)
                                     .ToArray();

                return Result<IEnumerable<string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve used environment keys for id: {formattedParams}: {e}");
                return Result<IEnumerable<string>>.Error($"failed to retrieve used environment keys for id: {formattedParams}: {e}",
                                                         ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<JToken>> GetJson(ConfigurationIdentifier identifier, DateTime when)
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
                                             .FirstOrDefaultAsync(c => c.ConfigEnvironment.Name == identifier.Environment.Name &&
                                                                       c.ConfigEnvironment.Category == identifier.Environment.Category &&
                                                                       c.Structure.Name == identifier.Structure.Name &&
                                                                       c.Structure.Version == identifier.Structure.Version);

                if (dbResult is null)
                    return Result<JToken>.Error($"no configuration found with id: {formattedParams}", ErrorCode.NotFound);

                try
                {
                    var result = JToken.Parse(dbResult.ConfigurationJson);

                    return Result<JToken>.Success(result);
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
                return Result<JToken>.Error($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                            ErrorCode.DbQueryError);
            }
        }
    }
}