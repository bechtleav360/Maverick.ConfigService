using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    [Route("api")]
    public class CompatibilityController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public CompatibilityController(IServiceProvider provider,
                                       ILogger<CompatibilityController> logger,
                                       IProjectionStore store,
                                       IJsonTranslator translator)
            : base(provider, logger)
        {
            _store = store;
            _translator = translator;
        }

        [HttpGet("configuration/service")]
        public async Task<IActionResult> GetConfigurationForService([FromQuery] string configurationType = null,
                                                                    [FromQuery] string separator = null,
                                                                    [FromQuery] string format = null)
        {
            var availableStructures = await _store.Structures.GetAvailableVersions(configurationType);

            var structureVersion = availableStructures.IsError
                                       ? 1
                                       : availableStructures.Data.Max();

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(
                                                                 new EnvironmentIdentifier("av360", "dev"),
                                                                 new StructureIdentifier(configurationType, structureVersion)),
                                                             DateTime.UtcNow);

            if (result.IsError)
            {
                Logger.LogWarning($"could not load data for '{configurationType}': {result.Code:D}({result.Code:G}) {result.Message}");
                return Ok(new Dictionary<string, string>());
            }

            if (format is null || format.Equals("list", StringComparison.OrdinalIgnoreCase))
                return Ok(new
                {
                    StatusCode = 0,
                    ErrorMessageCode = "",
                    Data = new[]
                    {
                        separator is null
                            ? result.Data
                            : result.Data
                                    .ToDictionary(k => k.Key.Replace("/", separator),
                                                  k => k.Value)
                    }
                });

            var json = _translator.ToJson(result.Data);

            return Ok(json);
        }
    }
}