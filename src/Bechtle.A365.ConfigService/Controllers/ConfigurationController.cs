using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// </summary>
    [Route(ApiBaseRoute + "configurations")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;

        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       IEventStore eventStore,
                                       IProjectionStore store)
            : base(provider, logger)
        {
            _eventStore = eventStore;
            _store = store;
        }

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableConfigurations()
        {
            var result = await _store.Configurations.GetAvailable();

            return Result(result);
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
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfiguration(string environmentCategory,
                                                          string environmentName,
                                                          string structureName,
                                                          int structureVersion)
        {
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier));

            return Result(result);
        }

        /// <summary>
        ///     create a new configuration built from a given Environment and Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        public async Task<IActionResult> BuildConfiguration(string environmentCategory,
                                                            string environmentName,
                                                            string structureName,
                                                            int structureVersion)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureId = new StructureIdentifier(structureName, structureVersion);

            await new ConfigSnapshot().IdentifiedBy(structureId, envId)
                                      .Create()
                                      .Save(_eventStore);

            return AcceptedAtAction(nameof(GetConfiguration), new {environmentCategory, environmentName, structureName, structureVersion});
        }
    }
}