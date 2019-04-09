using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     retrieve the history of various objects
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "history")]
    public class HistoryController : ControllerBase
    {
        private readonly V0.HistoryController _previousVersion;

        /// <inheritdoc />
        public HistoryController(IServiceProvider provider,
                                 ILogger<HistoryController> logger,
                                 V0.HistoryController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     get all keys within the environment and metadata of their last change
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("blame/environment/{category}/{name}", Name = ApiVersionFormatted + "Blame")]
        public Task<IActionResult> BlameEnvironment([FromRoute] string category,
                                                    [FromRoute] string name)
            => _previousVersion.BlameEnvironment(category, name);

        /// <summary>
        ///     get the complete history and metadata of an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("environment/{category}/{name}", Name = ApiVersionFormatted + "GetEnvironmentHistory")]
        public Task<IActionResult> GetEnvironmentHistory([FromRoute] string category,
                                                         [FromRoute] string name,
                                                         [FromQuery] string key = null)
            => _previousVersion.GetEnvironmentHistory(category, name, key);
    }
}