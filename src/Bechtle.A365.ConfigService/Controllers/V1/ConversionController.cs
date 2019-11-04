using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     convert Dictionary{string, string} to and from JSON
    /// </summary>
    [Route(ApiBaseRoute + "convert")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
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
        /// <param name="separator"></param>
        /// <returns></returns>
        [HttpPost("map/json", Name = "ConvertDictionaryToJson")]
        public IActionResult DictionaryToJson([FromBody] Dictionary<string, string> dictionary,
                                              [FromQuery] string separator = null)
        {
            try
            {
                if (dictionary is null)
                    return BadRequest("no dictionary received");

                var result = _translator.ToJson(dictionary, separator ?? JsonTranslatorDefaultSettings.Separator);

                Metrics.Measure.Counter.Increment(KnownMetrics.Conversion, "Map => Json");

                return Ok(result);
            }
            catch (Exception e)
            {
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, "failed to translate dictionary to json");
                return StatusCode(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        /// <summary>
        ///     convert the given json to map of path => value
        /// </summary>
        /// <param name="json"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        [HttpPost("json/map", Name = "ConvertJsonToDictionary")]
        public IActionResult JsonToDictionary([FromBody] JsonElement json,
                                              [FromQuery] string separator = null)
        {
            try
            {
                var result = _translator.ToDictionary(json, separator ?? JsonTranslatorDefaultSettings.Separator);

                Metrics.Measure.Counter.Increment(KnownMetrics.Conversion, "Json => Map");

                return Ok(result ?? new Dictionary<string, string>());
            }
            catch (Exception e)
            {
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, "failed to translate json to dictionary");
                return StatusCode(HttpStatusCode.InternalServerError, e.ToString());
            }
        }
    }
}