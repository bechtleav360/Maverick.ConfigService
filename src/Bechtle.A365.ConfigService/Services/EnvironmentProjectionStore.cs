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
        public async Task<Result<IList<EnvironmentIdentifier>>> GetAvailable()
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .ToListAsync();

                var result = dbResult?.Select(s => new EnvironmentIdentifier(s))
                                     .ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result<IList<EnvironmentIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result<IList<EnvironmentIdentifier>>.Error("failed to retrieve environments", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<EnvironmentIdentifier>>> GetAvailableInCategory(string category)
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .Where(s => s.Category == category)
                                             .OrderBy(s => s.Category)
                                             .ThenBy(s => s.Name)
                                             .ToListAsync();
                
                var result = dbResult?.Select(s => new EnvironmentIdentifier(s))
                                     .ToList()
                             ?? new List<EnvironmentIdentifier>();

                return Result<IList<EnvironmentIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve environments");
                return Result<IList<EnvironmentIdentifier>>.Error($"failed to retrieve environments in '{category}'", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier)
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                       s.Name == identifier.Name);

                if (dbResult is null)
                    return Result<IDictionary<string, string>>.Error($"no environment found with (" +
                                                                     $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name})", 
                                                                     ErrorCode.NotFound);

                var result = dbResult.Keys
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                return Result<IDictionary<string, string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for environment " +
                                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})");

                return Result<IDictionary<string, string>>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }
    }
}