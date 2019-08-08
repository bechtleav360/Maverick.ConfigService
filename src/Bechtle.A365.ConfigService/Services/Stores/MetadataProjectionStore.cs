using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class MetadataProjectionStore : IMetadataProjectionStore
    {
        private readonly IMemoryCache _cache;
        private readonly ProjectionStoreContext _context;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public MetadataProjectionStore(ILogger<MetadataProjectionStore> logger,
                                       ProjectionStoreContext context,
                                       IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata()
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(MetadataProjectionStore),
                                                       nameof(GetProjectedEventMetadata)),
                           async entry =>
                           {
                               var items = await _context.ProjectedEventMetadata
                                                         .OrderBy(m => m.Index)
                                                         .ToListAsync();

                               if (!(items is null))
                                   entry.SetDuration(CacheDuration.Tiny);

                               return Result.Success((IList<ProjectedEventMetadata>) items ?? new List<ProjectedEventMetadata>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve list of event-metadata");
                return Result.Error<IList<ProjectedEventMetadata>>("could not retrieve list of event-metadata", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata(Expression<Func<ProjectedEventMetadata, bool>> filter,
                                                                                            string filterName)
        {
            try
            {
                return await _cache.GetOrCreateAsync(
                           CacheUtilities.MakeCacheKey(nameof(IMetadataProjectionStore),
                                                       nameof(GetProjectedEventMetadata),
                                                       filterName),
                           async entry =>
                           {
                               var items = await _context.ProjectedEventMetadata
                                                         .Where(filter)
                                                         .OrderBy(m => m.Index)
                                                         .ToListAsync();

                               if (!(items is null))
                                   entry.SetDuration(CacheDuration.Tiny);

                               return Result.Success((IList<ProjectedEventMetadata>) items ?? new List<ProjectedEventMetadata>());
                           });
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve list of event-metadata");
                return Result.Error<IList<ProjectedEventMetadata>>("could not retrieve list of event-metadata", ErrorCode.DbQueryError);
            }
        }
    }
}