using System;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     search through the projected data
    /// </summary>
    [Route(ApiBaseRoute + "search")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class SearchController : ControllerBase
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public SearchController(IServiceProvider provider,
                                ILogger<SearchController> logger,
                                IProjectionStore store)
            : base(provider, logger)
        {
            _store = store;
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="query"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = "GetKeyAutocomplete")]
        public async Task<IActionResult> GetKeyAutocompleteList([FromRoute] string category,
                                                                [FromRoute] string name,
                                                                [FromQuery] string query = null,
                                                                [FromQuery] int offset = -1,
                                                                [FromQuery] int length = -1,
                                                                [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeyAutoComplete(identifier, query, range, targetVersion);

                return Result(result);
            }
            catch (Exception e)
            {
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, $"failed to export retrieve autocomplete-data (" +
                                   $"{nameof(category)}: {category}; " +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(query)}: {query}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion};)");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }
    }
}