using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
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
        ///     create a new configuration for each combination of given Environment and available structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var availableStructures = await _store.Structures.GetAvailable();

            if (availableStructures.IsError)
                return ProviderError(availableStructures);

            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);

            foreach (var structure in availableStructures.Data)
                await new ConfigSnapshot().IdentifiedBy(structure, envId)
                                          .ValidFrom(buildOptions?.ValidFrom)
                                          .ValidTo(buildOptions?.ValidTo)
                                          .Create()
                                          .Save(_eventStore);

            return Accepted();
        }

        /// <summary>
        ///     create a new configuration for each combination of given Environment and all versions of given Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromRoute] string structureName,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var availableStructures = await _store.Structures.GetAvailableVersions(structureName);

            if(availableStructures.IsError)
                return ProviderError(availableStructures);

            foreach (var structureId in availableStructures.Data
                                                           .Select(v => new StructureIdentifier(structureName, v)))
                await new ConfigSnapshot().IdentifiedBy(structureId, envId)
                                          .ValidFrom(buildOptions?.ValidFrom)
                                          .ValidTo(buildOptions?.ValidTo)
                                          .Create()
                                          .Save(_eventStore);

            return Accepted();
        }

        /// <summary>
        ///     create a new configuration built from a given Environment and Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="buildOptions">times are assumed to be UTC</param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromRoute] string structureName,
                                                            [FromRoute] int structureVersion,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureId = new StructureIdentifier(structureName, structureVersion);

            await new ConfigSnapshot().IdentifiedBy(structureId, envId)
                                      .ValidFrom(buildOptions?.ValidFrom)
                                      .ValidTo(buildOptions?.ValidTo)
                                      .Create()
                                      .Save(_eventStore);

            return AcceptedAtAction(nameof(GetConfiguration), new {environmentCategory, environmentName, structureName, structureVersion});
        }

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableConfigurations() => await GetAvailableConfigurations(DateTime.UtcNow);

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <returns></returns>
        [HttpGet("available/{when}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableConfigurations([FromRoute] DateTime when)
        {
            var result = await _store.Configurations.GetAvailable(when);

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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/keys")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfiguration([FromRoute] string environmentCategory,
                                                          [FromRoute] string environmentName,
                                                          [FromRoute] string structureName,
                                                          [FromRoute] int structureVersion)
            => await GetConfiguration(environmentCategory,
                                      environmentName,
                                      structureName,
                                      structureVersion,
                                      DateTime.UtcNow);

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/{when}/keys")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfiguration([FromRoute] string environmentCategory,
                                                          [FromRoute] string environmentName,
                                                          [FromRoute] string structureName,
                                                          [FromRoute] int structureVersion,
                                                          [FromRoute] DateTime when)
        {
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier), when);

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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/json")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigurationJson([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion)
            => await GetConfigurationJson(environmentCategory,
                                          environmentName,
                                          structureName,
                                          structureVersion,
                                          DateTime.UtcNow);

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/{when}/json")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigurationJson([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion,
                                                              [FromRoute] DateTime when)
        {
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier), when);

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
        [Obsolete]
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigurationObsolete([FromRoute] string environmentCategory,
                                                                  [FromRoute] string environmentName,
                                                                  [FromRoute] string structureName,
                                                                  [FromRoute] int structureVersion)
            => await GetConfiguration(environmentCategory,
                                      environmentName,
                                      structureName,
                                      structureVersion,
                                      DateTime.UtcNow);

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/{when}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigurationObsolete([FromRoute] string environmentCategory,
                                                                  [FromRoute] string environmentName,
                                                                  [FromRoute] string structureName,
                                                                  [FromRoute] int structureVersion,
                                                                  [FromRoute] DateTime when)
            => await GetConfiguration(environmentCategory,
                                      environmentName,
                                      structureName,
                                      structureVersion,
                                      when);
    }
}