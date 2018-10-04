using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailable()
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ThenBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .ToListAsync();

                if (dbResult is null)
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Select(s => new ConfigurationIdentifier(s))
                                     .ToList();

                if (!result.Any())
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithEnvironment(EnvironmentIdentifier environment)
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => c.ConfigEnvironment.Category == environment.Category &&
                                                         c.ConfigEnvironment.Name == environment.Name)
                                             .OrderBy(s => s.Structure.Name)
                                             .ThenByDescending(s => s.Structure.Version)
                                             .ToListAsync();

                if (dbResult is null)
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Select(s => new ConfigurationIdentifier(s))
                                     .ToList();

                if (!result.Any())
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<ConfigurationIdentifier>>> GetAvailableWithStructure(StructureIdentifier structure)
        {
            try
            {
                var dbResult = await _context.ProjectedConfigurations
                                             .Where(c => c.Structure.Name == structure.Name &&
                                                         c.Structure.Version == structure.Version)
                                             .OrderBy(s => s.ConfigEnvironment.Category)
                                             .ThenBy(s => s.ConfigEnvironment.Name)
                                             .ToListAsync();

                if (dbResult is null)
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Select(s => new ConfigurationIdentifier(s))
                                     .ToList();

                if (!result.Any())
                    return Result<IList<ConfigurationIdentifier>>.Error("no items found", ErrorCode.NotFound);

                return Result<IList<ConfigurationIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve projected configurations");
                return Result<IList<ConfigurationIdentifier>>.Error("failed to retrieve projected configurations", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeys(ConfigurationIdentifier identifier)
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
                _logger.LogError(e, $"failed to retrieve projected configuration keys for id: {formattedParams}");
                return Result<IDictionary<string, string>>.Error($"failed to retrieve projected configuration keys for id: {formattedParams}",
                                                                 ErrorCode.DbQueryError);
            }
        }
    }
}