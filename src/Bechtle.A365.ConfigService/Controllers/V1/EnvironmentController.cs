using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Query / Manage Environment-Data
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "environments")]
    public class EnvironmentController : ControllerBase
    {
        private readonly V0.EnvironmentController _previousVersion;

        /// <inheritdoc />
        public EnvironmentController(IServiceProvider provider,
                                     ILogger<EnvironmentController> logger,
                                     V0.EnvironmentController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     create a new Environment with the given Category + Name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost("{category}/{name}", Name = ApiVersionFormatted + "AddEnvironment")]
        public Task<IActionResult> AddEnvironment([FromRoute] string category,
                                                  [FromRoute] string name)
            => _previousVersion.AddEnvironment(category, name);

        /// <summary>
        ///     delete keys from the environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpDelete("{category}/{name}/keys", Name = ApiVersionFormatted + "DeleteFromEnvironment")]
        public Task<IActionResult> DeleteKeys([FromRoute] string category,
                                              [FromRoute] string name,
                                              [FromBody] string[] keys)
            => _previousVersion.DeleteKeys(category, name, keys);

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = ApiVersionFormatted + "GetAvailableEnvironments")]
        public Task<IActionResult> GetAvailableEnvironments([FromQuery] int offset = -1,
                                                            [FromQuery] int length = -1)
            => _previousVersion.GetAvailableEnvironments(offset, length);

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/keys", Name = ApiVersionFormatted + "GetEnvironmentAsKeys")]
        public Task<IActionResult> GetKeys([FromRoute] string category,
                                           [FromRoute] string name,
                                           [FromQuery] string filter,
                                           [FromQuery] int offset = -1,
                                           [FromQuery] int length = -1)
            => _previousVersion.GetKeys(category, name, filter, offset, length);

        /// <summary>
        ///     get the keys contained in an environment, converted to JSON
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/json", Name = ApiVersionFormatted + "GetEnvironmentAsJson")]
        public Task<IActionResult> GetKeysAsJson([FromRoute] string category,
                                                 [FromRoute] string name,
                                                 [FromQuery] string filter)
            => _previousVersion.GetKeysAsJson(category, name, filter);

        /// <summary>
        ///     get the keys contained in an environment including all their metadata
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="offset" />
        /// <param name="length" />
        /// <returns></returns>
        [HttpGet("{category}/{name}/keys/objects", Name = ApiVersionFormatted + "GetEnvironmentAsObjects")]
        public Task<IActionResult> GetKeysWithMetadata([FromRoute] string category,
                                                       [FromRoute] string name,
                                                       [FromQuery] string filter,
                                                       [FromQuery] int offset = -1,
                                                       [FromQuery] int length = -1)
            => _previousVersion.GetKeysWithMetadata(category, name, filter, offset, length);

        /// <summary>
        ///     add or update keys in the environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpPut("{category}/{name}/keys", Name = ApiVersionFormatted + "UpdateEnvironment")]
        public Task<IActionResult> UpdateKeys([FromRoute] string category,
                                              [FromRoute] string name,
                                              [FromBody] DtoConfigKey[] keys)
            => _previousVersion.UpdateKeys(category, name, keys);
    }
}