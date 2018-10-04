﻿using System;
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
        public async Task<Result<IList<StructureIdentifier>>> GetAvailable()
        {
            try
            {
                var dbResult = await _context.Structures
                                             .OrderBy(s => s.Name)
                                             .ThenByDescending(s => s.Version)
                                             .ToListAsync();

                if (dbResult is null)
                    return Result<IList<StructureIdentifier>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Select(s => new StructureIdentifier(s))
                                     .ToList();

                if (!result.Any())
                    return Result<IList<StructureIdentifier>>.Error("no items found", ErrorCode.NotFound);

                return Result<IList<StructureIdentifier>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result<IList<StructureIdentifier>>.Error("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IList<int>>> GetAvailableVersions(string name)
        {
            try
            {
                var dbResult = await _context.Structures
                                             .Where(s => s.Name == name)
                                             .OrderBy(s => s.Name)
                                             .ThenByDescending(s => s.Version)
                                             .ToListAsync();

                if (dbResult is null)
                    return Result<IList<int>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Select(s => s.Version)
                                     .ToList();

                if (!result.Any())
                    return Result<IList<int>>.Error("no items found", ErrorCode.NotFound);

                return Result<IList<int>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result<IList<int>>.Error("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<Result<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier)
        {
            try
            {
                var dbResult = await _context.Structures
                                             .FirstOrDefaultAsync(s => s.Name == identifier.Name &&
                                                                       s.Version == identifier.Version);

                if (dbResult is null)
                    return Result<IDictionary<string, string>>.Error("no items found", ErrorCode.NotFound);

                var result = dbResult.Keys
                                     .ToImmutableSortedDictionary(k => k.Key,
                                                                  k => k.Value,
                                                                  StringComparer.OrdinalIgnoreCase);

                if (!result.Any())
                    return Result<IDictionary<string, string>>.Error("no items found", ErrorCode.NotFound);

                return Result<IDictionary<string, string>>.Success(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for structure " +
                                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})");

                return Result<IDictionary<string, string>>.Error(
                    "failed to retrieve keys for structure " +
                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})",
                    ErrorCode.DbQueryError);
            }
        }
    }
}