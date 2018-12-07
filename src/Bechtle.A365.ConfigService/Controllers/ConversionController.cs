using System;
using System.Collections.Generic;
using System.Net;
using Bechtle.A365.ConfigService.Common.Converters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     convert Dictionary{string, string} to and from JSON
    /// </summary>
    [Route(ApiBaseRoute + "convert")]
    public class ConversionController : ControllerBase
    {
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public ConversionController(IServiceProvider provider,
                                    ILogger<ConversionController> logger,
                                    IJsonTranslator translator)
            : base(provider, logger)
        {
            _translator = translator;
        }

        /// <summary>
        ///     convert the given map to the appropriate JSON
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        [HttpPost("map/json")]
        public IActionResult DictionaryToJson([FromBody] Dictionary<string, string> dictionary)
        {
            try
            {
                if (dictionary is null)
                    return Ok(new JObject());

                var result = _translator.ToJson(dictionary);

                return Ok(result ?? new JObject());
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to translate dictionary to json: {e}");
                return StatusCode(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        /// <summary>
        ///     convert the given json to map of path => value
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("json/map")]
        public IActionResult JsonToDictionary([FromBody] JToken json)
        {
            try
            {
                if (json is null)
                    return Ok(new Dictionary<string, string>());

                var result = _translator.ToDictionary(json);

                return Ok(result ?? new Dictionary<string, string>());
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to translate json to dictionary: {e}");
                return StatusCode(HttpStatusCode.InternalServerError, e.ToString());
            }
        }
    }
}