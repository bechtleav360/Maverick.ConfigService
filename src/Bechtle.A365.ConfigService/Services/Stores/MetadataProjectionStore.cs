using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <inheritdoc />
    public class MetadataProjectionStore : IMetadataProjectionStore
    {
        private readonly ILogger _logger;
        private readonly ProjectionStoreContext _context;

        /// <inheritdoc />
        public MetadataProjectionStore(ILogger<MetadataProjectionStore> logger,
                                       ProjectionStoreContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata()
        {
            try
            {
                var items = await _context.ProjectedEventMetadata
                                          .OrderBy(m => m.Index)
                                          .ToListAsync()
                            ?? new List<ProjectedEventMetadata>();

                return Result.Success((IList<ProjectedEventMetadata>) items);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve list of event-metadata");
                return Result.Error<IList<ProjectedEventMetadata>>("could not retrieve list of event-metadata", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata(Expression<Func<ProjectedEventMetadata, bool>> filter)
        {
            try
            {
                var items = await _context.ProjectedEventMetadata
                                          .Where(filter)
                                          .OrderBy(m => m.Index)
                                          .ToListAsync()
                            ?? new List<ProjectedEventMetadata>();

                return Result.Success((IList<ProjectedEventMetadata>) items);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve list of event-metadata");
                return Result.Error<IList<ProjectedEventMetadata>>("could not retrieve list of event-metadata", ErrorCode.DbQueryError);
            }
        }
    }
}