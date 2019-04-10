using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     search through the projected data
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "search")]
    public class SearchController : VersionedController<V0.Controllers.SearchController>
    {
        /// <inheritdoc />
        public SearchController(IServiceProvider provider,
                                ILogger<SearchController> logger,
                                V0.Controllers.SearchController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     get a list of possible next options from the given query
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="query"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = ApiVersionFormatted + "GetKeyAutocomplete")]
        public Task<IActionResult> GetKeyAutocompleteList([FromRoute] string category,
                                                          [FromRoute] string name,
                                                          [FromQuery] string query = null,
                                                          [FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
            => PreviousVersion.GetKeyAutocompleteList(category, name, query, offset, length);
    }
}