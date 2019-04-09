using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     search through the projected data
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "search")]
    public class SearchController : ControllerBase
    {
        private new const string ApiVersion = "1.0";
        private readonly V0.SearchController _previousVersion;

        /// <inheritdoc />
        public SearchController(IServiceProvider provider,
                                ILogger<SearchController> logger,
                                V0.SearchController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
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
            => _previousVersion.GetKeyAutocompleteList(category, name, query, offset, length);
    }
}