using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     read metadata for projected events
    /// </summary>
    public interface IMetadataProjectionStore
    {
        /// <summary>
        ///     get a list of metadata objects for the already projected domain-events
        /// </summary>
        /// <returns></returns>
        Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata();

        /// <summary>
        ///     get a list of metadata objects for the already projected domain-events
        /// </summary>
        /// <param name="filter">filter applied to all items</param>
        /// <param name="filterName">unique filter-name, used to cache the results</param>
        /// <returns></returns>
        Task<IResult<IList<ProjectedEventMetadata>>> GetProjectedEventMetadata(Expression<Func<ProjectedEventMetadata, bool>> filter, string filterName);
    }
}