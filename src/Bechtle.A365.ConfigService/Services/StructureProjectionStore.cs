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
    public class StructureProjectionStore : IStructureProjectionStore
    {
        private readonly ProjectionStoreContext _context;
        private readonly ILogger<StructureProjectionStore> _logger;

        /// <inheritdoc />
        public StructureProjectionStore(ProjectionStoreContext context, ILogger<StructureProjectionStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<StructureIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                var dbResult = await _context.Structures
                                             .OrderBy(s => s.Name)
                                             .ThenByDescending(s => s.Version)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .Select(s => new StructureIdentifier(s.Name, s.Version))
                                             .ToListAsync();

                var result = dbResult?.ToList()
                             ?? new List<StructureIdentifier>();

                return Result.Success<IList<StructureIdentifier>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve structures: {e}");
                return Result.Error<IList<StructureIdentifier>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<int>>> GetAvailableVersions(string name, QueryRange range)
        {
            try
            {
                var dbResult = await _context.Structures
                                             .Where(s => s.Name == name)
                                             .OrderBy(s => s.Name)
                                             .ThenByDescending(s => s.Version)
                                             .Skip(range.Offset)
                                             .Take(range.Length)
                                             .ToListAsync();

                var result = dbResult?.Select(s => s.Version)
                                     .ToList()
                             ?? new List<int>();

                return Result.Success<IList<int>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to retrieve structures: {e}");
                return Result.Error<IList<int>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                var dbResult = await _context.FullStructures
                                             .FirstOrDefaultAsync(s => s.Name == identifier.Name &&
                                                                       s.Version == identifier.Version);

                if (dbResult is null)
                    return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                     $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                     ")",
                                                                     ErrorCode.NotFound);

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
                var dbResult = await _context.FullStructures
                                             .FirstOrDefaultAsync(s => s.Name == identifier.Name &&
                                                                       s.Version == identifier.Version);

                if (dbResult is null)
                    return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                     $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                     ")",
                                                                     ErrorCode.NotFound);

                var result = dbResult.Variables
                                     .OrderBy(v => v.Key)
                                     .Skip(range.Offset)
                                     .Take(range.Length)
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
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