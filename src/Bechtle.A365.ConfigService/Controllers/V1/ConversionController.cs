using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     convert Dictionary{string, string} to and from JSON
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "convert")]
    public class ConversionController : ControllerBase
    {
        private readonly V0.ConversionController _previousVersion;

        /// <inheritdoc />
        public ConversionController(IServiceProvider provider,
                                    ILogger<ConversionController> logger,
                                    V0.ConversionController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     convert the given map to the appropriate JSON
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        [HttpPost("map/json", Name = ApiVersionFormatted + "ConvertDictionaryToJson")]
        public IActionResult DictionaryToJson([FromBody] Dictionary<string, string> dictionary,
                                              [FromQuery] string separator = null)
            => _previousVersion.DictionaryToJson(dictionary, separator);

        /// <summary>
        ///     convert the given json to map of path => value
        /// </summary>
        /// <param name="json"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        [HttpPost("json/map", Name = ApiVersionFormatted + "ConvertJsonToDictionary")]
        public IActionResult JsonToDictionary([FromBody] JToken json,
                                              [FromQuery] string separator = null)
            => _previousVersion.JsonToDictionary(json, separator);
    }
}