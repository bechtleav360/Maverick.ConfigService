using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services.Stores;
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
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}/keys/autocomplete", Name = "GetKeyAutocomplete")]
        public async Task<IActionResult> GetKeyAutocompleteList([FromRoute] string category,
                                                                [FromRoute] string name,
                                                                [FromQuery] string query = null,
                                                                [FromQuery] int offset = -1,
                                                                [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);

            var result = await _store.Environments.GetKeyAutoComplete(identifier, query, range);

            return Result(result);
        }
    }
}