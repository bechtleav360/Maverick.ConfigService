using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;




namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     Controller responsible to read / write its own Configuration.
    /// </summary>
    [Route(ApiBaseRoute + "configurations/custom")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class SelfConfigurationController : ControllerBase
    {
        private const string ConfigFileLocation = "data/appsettings.json";
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public SelfConfigurationController(IServiceProvider provider,
                                           ILogger<SelfConfigurationController> logger,
                                           IJsonTranslator translator)
            : base(provider, logger)
        {
            _translator = translator;
        }

        /// <summary>
        ///     add data to the used configuration
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> AppendConfiguration([FromBody] JToken json)
        {
            var givenKeys = _translator.ToDictionary(json);
            IDictionary<string, string> currentKeys = null;

            var converter = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                Converters = {new StringEnumConverter(), new IsoDateTimeConverter()}
            };

            try
            {
                if (System.IO.File.Exists(ConfigFileLocation))
                {
                    using var file = System.IO.File.OpenText(ConfigFileLocation);
                    using var reader = new JsonTextReader(file);

                    var currentJson = converter.Deserialize<JToken>(reader);
                    currentKeys = _translator.ToDictionary(currentJson);
                }
                else
                {
                    currentKeys = new Dictionary<string, string>();
                }
            }
            catch (IOException e)
            {
                Logger.LogWarning(e, $"IO-Error while reading file '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "internal error while reading configuration from disk");
            }
            catch (JsonException e)
            {
                Logger.LogWarning(e, $"could not deserialize configuration from '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "deserialization error while reading configuration, try overwriting it");
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"dump configuration from '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "unidentified error occured");
            }

            var resultKeys = currentKeys;
            foreach (var (key, value) in givenKeys)
                resultKeys[key] = value;

            var resultJson = _translator.ToJson(resultKeys);

            try
            {
                // create directory-structure if it doesn't already exist
                var fileInfo = new FileInfo(ConfigFileLocation);
                if (!Directory.Exists(fileInfo.DirectoryName))
                    Directory.CreateDirectory(fileInfo.DirectoryName);

                await using var file = System.IO.File.CreateText(ConfigFileLocation);
                using var writer = new JsonTextWriter(file);

                converter.Serialize(writer, resultJson);

                await writer.FlushAsync();
                await file.FlushAsync();

                return Ok();
            }
            catch (IOException e)
            {
                Logger.LogWarning(e, $"IO-Error while writing file '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "could not write configuration back to disk");
            }
        }

        /// <summary>
        ///     dump the writable configuration
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult DumpConfiguration()
        {
            try
            {
                if (!System.IO.File.Exists(ConfigFileLocation))
                    return Ok(new JObject());

                using var file = System.IO.File.OpenText(ConfigFileLocation);
                using var reader = new JsonTextReader(file);

                var converter = new JsonSerializer();
                var json = converter.Deserialize<JToken>(reader);
                return Ok(json.ToString(Formatting.Indented));
            }
            catch (IOException e)
            {
                Logger.LogWarning(e, $"IO-Error while reading file '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "internal error while reading configuration from disk");
            }
            catch (JsonException e)
            {
                Logger.LogWarning(e, $"could not deserialize configuration from '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "deserialization error while reading configuration, try overwriting it");
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"dump configuration from '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "unidentified error occured");
            }
        }

        /// <summary>
        ///     overwrite the current config-file with the data given
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SetConfiguration([FromBody] JToken json)
        {
            try
            {
                // create directory-structure if it doesn't already exist
                var fileInfo = new FileInfo(ConfigFileLocation);
                if (!Directory.Exists(fileInfo.DirectoryName))
                    Directory.CreateDirectory(fileInfo.DirectoryName);

                await System.IO.File.WriteAllTextAsync(ConfigFileLocation,
                                                       json.ToString(Formatting.Indented,
                                                                     new StringEnumConverter(),
                                                                     new IsoDateTimeConverter()));

                return Ok();
            }
            catch (JsonException e)
            {
                Logger.LogWarning(e, "serialization error while writing configuration back to disk");
                return StatusCode(HttpStatusCode.BadRequest, $"serialization error while writing configuration: {e.Message}");
            }
            catch (IOException e)
            {
                Logger.LogWarning(e, $"IO-Error while writing file '{ConfigFileLocation}'");
                return StatusCode(HttpStatusCode.InternalServerError, "could not write configuration back to disk");
            }
        }
    }
}