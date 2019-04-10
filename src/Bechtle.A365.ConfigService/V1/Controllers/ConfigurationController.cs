using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     Query / Build Service-Configurations
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "configurations")]
    public class ConfigurationController : VersionedController<V0.Controllers.ConfigurationController>
    {
        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       V0.Controllers.ConfigurationController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     create a new configuration for each combination of given Environment and available structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}", Name = ApiVersionFormatted + "BuildConfigurationsForAllStructures")]
        public Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                      [FromRoute] string environmentName,
                                                      [FromBody] ConfigurationBuildOptions buildOptions)
            => PreviousVersion.BuildConfiguration(environmentCategory, environmentName, buildOptions);

        /// <summary>
        ///     create a new configuration for each combination of given Environment and all versions of given Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}", Name = ApiVersionFormatted + "BuildConfigurationsForAllVersionsOfStructure")]
        public Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromRoute] string structureName,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
            => PreviousVersion.BuildConfiguration(environmentCategory, environmentName, structureName, buildOptions);

        /// <summary>
        ///     create a new configuration built from a given Environment and Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="buildOptions">times are assumed to be UTC</param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = ApiVersionFormatted + "BuildConfiguration")]
        public Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                      [FromRoute] string environmentName,
                                                      [FromRoute] string structureName,
                                                      [FromRoute] int structureVersion,
                                                      [FromBody] ConfigurationBuildOptions buildOptions)
            => PreviousVersion.BuildConfiguration(environmentCategory, environmentName, structureName, structureVersion, buildOptions);

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <param name="when"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = ApiVersionFormatted + "GetAvailableConfigurations")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        public Task<IActionResult> GetAvailableConfigurations([FromQuery] DateTime when,
                                                              [FromQuery] int offset = -1,
                                                              [FromQuery] int length = -1)
            => PreviousVersion.GetAvailableConfigurations(when, offset, length);

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/keys", Name = ApiVersionFormatted + "GetConfigurationKeys")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public Task<IActionResult> GetConfiguration([FromRoute] string environmentCategory,
                                                    [FromRoute] string environmentName,
                                                    [FromRoute] string structureName,
                                                    [FromRoute] int structureVersion,
                                                    [FromQuery] DateTime when,
                                                    [FromQuery] int offset = -1,
                                                    [FromQuery] int length = -1)
            => PreviousVersion.GetConfiguration(environmentCategory,
                                                environmentName,
                                                structureName,
                                                structureVersion,
                                                when,
                                                offset,
                                                length);

        /// <summary>
        ///     get the keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/json", Name = ApiVersionFormatted + "GetConfigurationJson")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public Task<IActionResult> GetConfigurationJson([FromRoute] string environmentCategory,
                                                        [FromRoute] string environmentName,
                                                        [FromRoute] string structureName,
                                                        [FromRoute] int structureVersion,
                                                        [FromQuery] DateTime when)
            => PreviousVersion.GetConfigurationJson(environmentCategory,
                                                    environmentName,
                                                    structureName,
                                                    structureVersion,
                                                    when);

        /// <summary>
        ///     get configurations whose keys are stale, that should be re-built
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("stale", Name = ApiVersionFormatted + "GetStaleConfigurations")]
        public Task<IActionResult> GetStaleConfigurations([FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
            => PreviousVersion.GetStaleConfigurations(offset, length);

        /// <summary>
        ///     get the used environment-keys of a specific configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/usedKeys", Name = ApiVersionFormatted + "GetUsedEnvironmentKeys")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public Task<IActionResult> GetUsedKeys([FromRoute] string environmentCategory,
                                               [FromRoute] string environmentName,
                                               [FromRoute] string structureName,
                                               [FromRoute] int structureVersion,
                                               [FromQuery] DateTime when,
                                               [FromQuery] int offset = -1,
                                               [FromQuery] int length = -1)
            => PreviousVersion.GetUsedKeys(environmentCategory,
                                           environmentName,
                                           structureName,
                                           structureVersion,
                                           when,
                                           offset,
                                           length);
    }
}