using System;
using System.Collections.Generic;
using System.Net;
using App.Metrics;
using App.Metrics.Counter;
using Bechtle.A365.ConfigService.Common.Converters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     convert Dictionary{string, string} to and from JSON
    /// </summary>
    [Route(ApiBaseRoute + "convert")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class ConversionController : ControllerBase
    {
        private readonly IJsonTranslator _translator;
        private readonly IMetrics _metrics;

        /// <inheritdoc />
        public ConversionController(IServiceProvider provider,
                                    ILogger<ConversionController> logger,
                                    IJsonTranslator translator,
                                    IMetrics metrics)
            : base(provider, logger)
        {
            _translator = translator;
            _metrics = metrics;
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
                    return Ok(new JObject());

                var result = _translator.ToJson(dictionary, separator ?? JsonTranslatorDefaultSettings.Separator);

                _metrics.Measure.Counter.Increment(KnownMetrics.Conversion, "Map => Json");

                return Ok(result ?? new JObject());
            }
            catch (Exception e)
            {
                _metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
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
        public IActionResult JsonToDictionary([FromBody] JToken json,
                                              [FromQuery] string separator = null)
        {
            try
            {
                if (json is null)
                    return Ok(new Dictionary<string, string>());

                var result = _translator.ToDictionary(json, separator ?? JsonTranslatorDefaultSettings.Separator);

                _metrics.Measure.Counter.Increment(KnownMetrics.Conversion, "Json => Map");

                return Ok(result ?? new Dictionary<string, string>());
            }
            catch (Exception e)
            {
                _metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, "failed to translate json to dictionary");
                return StatusCode(HttpStatusCode.InternalServerError, e.ToString());
            }
        }
    }
}