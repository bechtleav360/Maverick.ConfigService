using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// </summary>
    [Route("configurations")]
    public class ConfigurationController : Controller
    {
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public ConfigurationController(IProjectionStore store)
        {
            _store = store;
        }

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        public async Task<ActionResult<IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>>> GetAvailableConfigurations()
        {
            var result = await _store.Configurations.GetAvailable();

            return Ok(result);
        }

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        public async Task<ActionResult<IDictionary<string, string>>> GetConfiguration(string environmentCategory,
                                                                                      string environmentName,
                                                                                      string structureName,
                                                                                      int structureVersion)
        {
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier));

            return Ok(result);
        }
    }
}