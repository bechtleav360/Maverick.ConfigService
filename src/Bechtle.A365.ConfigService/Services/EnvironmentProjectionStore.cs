using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public async Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjects(EnvironmentIdentifier identifier, QueryRange range)

        {
            try
            {
                var environment = await _context.ConfigEnvironments
                                                .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                          s.Name == identifier.Name);

                if (environment is null)
                    return Result<IEnumerable<DtoConfigKey>>.Error("no environment found with (" +
                                                                   $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                   $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                   ErrorCode.NotFound);

                var result = environment.Keys
                                        .OrderBy(k => k.Key)
                                        .Skip(range.Offset)
                                        .Take(range.Length)
                                        .Select(k => new DtoConfigKey
                                        {
                                            Key = k.Key,
                                            Value = k.Value,
                                            Description = k.Description,
                                            Type = k.Type
                                        })
                                        .ToArray();

                return Result<IEnumerable<DtoConfigKey>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IEnumerable<DtoConfigKey>>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<DtoConfigKey>>> GetKeyObjectsWithInheritance(EnvironmentIdentifier identifier, QueryRange range)
        {
            try
            {
                var environment = await _context.ConfigEnvironments
                                                .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                          s.Name == identifier.Name);

                if (environment is null)
                    return Result<IEnumerable<DtoConfigKey>>.Error("no environment found with (" +
                                                                   $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                   $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                   ErrorCode.NotFound);

                var defaultEnvironment = await _context.ConfigEnvironments
                                                       .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                                 s.Name == "Default");

                var defaultEnvironmentKeys = defaultEnvironment?.Keys ?? new List<ConfigEnvironmentKey>();
                var environmentKeys = environment.Keys ?? new List<ConfigEnvironmentKey>();

                IDictionary<string, DtoConfigKey> result = new Dictionary<string, DtoConfigKey>(Math.Max(environmentKeys.Count, defaultEnvironmentKeys.Count));

                foreach (var item in defaultEnvironmentKeys)
                    result[item.Key] = new DtoConfigKey
                    {
                        Key = item.Key,
                        Value = item.Value,
                        Description = item.Description,
                        Type = item.Type
                    };

                foreach (var item in environmentKeys)
                    result[item.Key] = new DtoConfigKey
                    {
                        Key = item.Key,
                        Value = item.Value,
                        Description = item.Description,
                        Type = item.Type
                    };

                return Result<IEnumerable<DtoConfigKey>>.Success(result.Values
                                                                       .OrderBy(k => k.Key)
                                                                       .ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IEnumerable<DtoConfigKey>>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeys(EnvironmentIdentifier identifier, QueryRange range)
        {
            try
            {
                var dbResult = await _context.ConfigEnvironments
                                             .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                       s.Name == identifier.Name);

                if (dbResult is null)
                    return Result<IDictionary<string, string>>.Error("no environment found with (" +
                                                                     $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                     ErrorCode.NotFound);

                var result = dbResult.Keys
                                     .OrderBy(k => k.Key)
                                     .Skip(range.Offset)
                                     .Take(range.Length)
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                return Result<IDictionary<string, string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IDictionary<string, string>>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeysWithInheritance(EnvironmentIdentifier identifier, QueryRange range)
        {
            try
            {
                var environment = await _context.ConfigEnvironments
                                                .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                          s.Name == identifier.Name);

                if (environment is null)
                    return Result<IDictionary<string, string>>.Error("no environment found with (" +
                                                                     $"{nameof(identifier.Category)}: {identifier.Category}; " +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name})",
                                                                     ErrorCode.NotFound);

                var defaultEnvironment = await _context.ConfigEnvironments
                                                       .FirstOrDefaultAsync(s => s.Category == identifier.Category &&
                                                                                 s.Name == "Default");

                var defaultEnvironmentKeys = defaultEnvironment?.Keys
                                                               ?.OrderBy(k => k.Key)
                                                               .Skip(range.Offset)
                                                               .Take(range.Length)
                                                               .ToList()
                                             ?? new List<ConfigEnvironmentKey>();

                var environmentKeys = environment.Keys
                                                 ?.OrderBy(k => k.Key)
                                                 .Skip(range.Offset)
                                                 .Take(range.Length)
                                                 .ToList()
                                      ?? new List<ConfigEnvironmentKey>();

                IDictionary<string, string> result = new Dictionary<string, string>(Math.Max(environmentKeys.Count, defaultEnvironmentKeys.Count));

                foreach (var item in defaultEnvironmentKeys)
                    result[item.Key] = item.Value;

                foreach (var item in environmentKeys)
                    result[item.Key] = item.Value;

                result = result.ToImmutableSortedDictionary(k => k.Key,
                                                            k => k.Value,
                                                            StringComparer.OrdinalIgnoreCase);

                return Result<IDictionary<string, string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for environment " +
                                 $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name}): {e}");

                return Result<IDictionary<string, string>>.Error(
                    "failed to retrieve keys for environment " +
                    $"({nameof(identifier.Category)}: {identifier.Category}; {nameof(identifier.Name)}: {identifier.Name})",
                    ErrorCode.DbQueryError);
            }
        }
    }
}