using System;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     search through the projected data
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "search")]
    public class SearchController : V0.SearchController
    {
        private new const string ApiVersion = "1.0";

        /// <inheritdoc />
        public SearchController(IServiceProvider provider,
                                ILogger<SearchController> logger,
                                IProjectionStore store)
            : base(provider, logger, store)
        {
        }
    }
}