using System;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
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
        public SearchController(ILogger<SearchController> logger,
                                IProjectionStore store)
            : base(logger)
        {
            _store = store;
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="category">Environment-Category to use for the query</param>
        /// <param name="name">Environment-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int) HttpStatusCode.OK)]
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = "GetEnvironmentKeyAutocomplete")]
        public async Task<IActionResult> GetEnvironmentKeyAutocompleteList([FromRoute] string category,
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
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve autocomplete-data (" +
                                   $"{nameof(category)}: {category}; " +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(query)}: {query}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion};)");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve autocomplete-data");
            }
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="name">Layer-Name to use for the query</param>
        /// <param name="query">search-query, written as incomplete path</param>
        /// <param name="offset">0-based offset from the beginning of the search-result</param>
        /// <param name="length">query-result size</param>
        /// <param name="targetVersion">Event-Version to use for this operation</param>
        /// <returns>list of Keys that match the given query</returns>
        [ProducesResponseType(typeof(DtoConfigKeyCompletion), (int) HttpStatusCode.OK)]
        [HttpGet("layer/{name}/keys/autocomplete", Name = "GetLayerKeyAutocomplete")]
        public async Task<IActionResult> GetLayerKeyAutocompleteList([FromRoute] string name,
                                                                     [FromQuery] string query = null,
                                                                     [FromQuery] int offset = -1,
                                                                     [FromQuery] int length = -1,
                                                                     [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new LayerIdentifier(name);

                var result = await _store.Layers.GetKeyAutoComplete(identifier, query, range, targetVersion);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve autocomplete-data (" +
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