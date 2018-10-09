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
    ///     used for dev-tasks
    /// </summary>
    [Route(ApiBaseRoute + "dev")]
    public class DevController : ControllerBase
    {
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public DevController(IServiceProvider provider,
                             ILogger<DevController> logger,
                             IJsonTranslator translator)
            : base(provider, logger)
        {
            _translator = translator;
        }

        /// <summary>
        ///     convert the given json to list of key => value pairs
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("json/to/dictionary")]
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
                Logger.LogError(e, "failed to translate json to dictionary");
                return StatusCode(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        ///     convert the given dictionary to the appropriate JSON
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        [HttpPost("dictionary/to/json")]
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
                Logger.LogError(e, "failed to translate dictionary to json");
                return StatusCode(HttpStatusCode.InternalServerError);
            }
        }
    }
}