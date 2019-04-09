﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V0
{
    /// <summary>
    ///     Query / Build Service-Configurations
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "configurations")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       IEventStore eventStore,
                                       IProjectionStore store,
                                       IJsonTranslator translator)
            : base(provider, logger)
        {
            _eventStore = eventStore;
            _store = store;
            _translator = translator;
        }

        /// <summary>
        ///     create a new configuration for each combination of given Environment and available structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}", Name = ApiVersionFormatted + "BuildConfigurationsForAllStructures")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var buildError = ValidateBuildOptions(buildOptions);
            if (!(buildError is null))
                return buildError;

            var availableStructures = await _store.Structures.GetAvailable(QueryRange.All);
            if (availableStructures.IsError)
                return ProviderError(availableStructures);

            var availableEnvironments = await _store.Environments.GetAvailable(QueryRange.All);
            if (availableEnvironments.IsError)
                return ProviderError(availableEnvironments);

            var environment = availableEnvironments.Data
                                                   .FirstOrDefault(e => e.Category.Equals(environmentCategory, StringComparison.InvariantCultureIgnoreCase) &&
                                                                        e.Name.Equals(environmentName, StringComparison.InvariantCultureIgnoreCase));

            if (environment is null)
                return NotFound($"no environment '{environmentCategory}/{environmentName}' found");

            var envId = new EnvironmentIdentifier(environment.Category, environment.Name);

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
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}", Name = ApiVersionFormatted + "BuildConfigurationsForAllVersionsOfStructure")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromRoute] string structureName,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var buildError = ValidateBuildOptions(buildOptions);
            if (!(buildError is null))
                return buildError;

            var availableStructures = await _store.Structures.GetAvailable(QueryRange.All);
            if (availableStructures.IsError)
                return ProviderError(availableStructures);

            var availableEnvironments = await _store.Environments.GetAvailable(QueryRange.All);
            if (availableEnvironments.IsError)
                return ProviderError(availableEnvironments);

            var environment = availableEnvironments.Data
                                                   .FirstOrDefault(e => e.Category.Equals(environmentCategory, StringComparison.InvariantCultureIgnoreCase) &&
                                                                        e.Name.Equals(environmentName, StringComparison.InvariantCultureIgnoreCase));

            if (environment is null)
                return NotFound($"no environment '{environmentCategory}/{environmentName}' found");

            var envId = new EnvironmentIdentifier(environment.Category, environment.Name);

            var structures = availableStructures.Data
                                                .Where(s => s.Name.Equals(structureName))
                                                .OrderBy(s => s.Version)
                                                .Select(s => new StructureIdentifier(s.Name, s.Version))
                                                .ToArray();

            if (!structures.Any())
                return NotFound($"no versions of structure '{structureName}' found");

            foreach (var structureId in structures)
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
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = ApiVersionFormatted + "BuildConfiguration")]
        public async Task<IActionResult> BuildConfiguration([FromRoute] string environmentCategory,
                                                            [FromRoute] string environmentName,
                                                            [FromRoute] string structureName,
                                                            [FromRoute] int structureVersion,
                                                            [FromBody] ConfigurationBuildOptions buildOptions)
        {
            var buildError = ValidateBuildOptions(buildOptions);
            if (!(buildError is null))
                return buildError;

            var availableStructures = await _store.Structures.GetAvailable(QueryRange.All);
            if (availableStructures.IsError)
                return ProviderError(availableStructures);

            var availableEnvironments = await _store.Environments.GetAvailable(QueryRange.All);
            if (availableEnvironments.IsError)
                return ProviderError(availableEnvironments);

            var environment = availableEnvironments.Data
                                                   .FirstOrDefault(e => e.Category.Equals(environmentCategory, StringComparison.InvariantCultureIgnoreCase) &&
                                                                        e.Name.Equals(environmentName, StringComparison.InvariantCultureIgnoreCase));

            if (environment is null)
                return NotFound($"no environment '{environmentCategory}/{environmentName}' found");

            var envId = new EnvironmentIdentifier(environment.Category, environment.Name);

            var structure = availableStructures.Data
                                               .FirstOrDefault(s => s.Name.Equals(structureName) &&
                                                                    s.Version == structureVersion);

            if (structure is null)
                return NotFound($"no versions of structure '{structureName}' found");

            var structureId = new StructureIdentifier(structure.Name, structure.Version);

            await new ConfigSnapshot().IdentifiedBy(structureId, envId)
                                      .ValidFrom(buildOptions?.ValidFrom)
                                      .ValidTo(buildOptions?.ValidTo)
                                      .Create()
                                      .Save(_eventStore);

            return AcceptedAtAction(nameof(GetConfiguration), new
            {
                EnvironmentName = environment.Category,
                EnvironmentCategory = environment.Name,
                StructureName = structure.Name,
                StructureVersion = structure.Version
            });
        }

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
        public async Task<IActionResult> GetAvailableConfigurations([FromQuery] DateTime when,
                                                                    [FromQuery] int offset = -1,
                                                                    [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var result = await _store.Configurations.GetAvailable(when, range);

            return Result(result);
        }

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
        public async Task<IActionResult> GetConfiguration([FromRoute] string environmentCategory,
                                                          [FromRoute] string environmentName,
                                                          [FromRoute] string structureName,
                                                          [FromRoute] int structureVersion,
                                                          [FromQuery] DateTime when,
                                                          [FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier), when, range);

            return Result(result);
        }

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
        public async Task<IActionResult> GetConfigurationJson([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion,
                                                              [FromQuery] DateTime when)
        {
            try
            {
                var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
                var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

                var result = await _store.Configurations.GetKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier), when, QueryRange.All);

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                if (json is null)
                    return StatusCode(HttpStatusCode.InternalServerError, "failed to translate keys to json");

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError("failed to retrieve configuration for (" +
                                $"{nameof(environmentCategory)}: {environmentCategory}, " +
                                $"{nameof(environmentName)}: {environmentName}, " +
                                $"{nameof(structureName)}: {structureName}, " +
                                $"{nameof(structureVersion)}: {structureVersion}, " +
                                $"{nameof(when)}: {when:O}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = ApiVersionFormatted + "GetConfigurationObsolete")]
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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/{when}",
            Name = ApiVersionFormatted + "GetConfigurationObsoleteAtPointInTime")]
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

        /// <summary>
        ///     get configurations whose keys are stale, that should be re-built
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("stale", Name = ApiVersionFormatted + "GetStaleConfigurations")]
        public async Task<IActionResult> GetStaleConfigurations([FromQuery] int offset = -1,
                                                                [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var result = await _store.Configurations.GetStale(range);

            return Result(result);
        }

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
        public async Task<IActionResult> GetUsedKeys([FromRoute] string environmentCategory,
                                                     [FromRoute] string environmentName,
                                                     [FromRoute] string structureName,
                                                     [FromRoute] int structureVersion,
                                                     [FromQuery] DateTime when,
                                                     [FromQuery] int offset = -1,
                                                     [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var result = await _store.Configurations.GetUsedConfigurationKeys(new ConfigurationIdentifier(envIdentifier, structureIdentifier), when, range);

            return Result(result);
        }

        /// <summary>
        ///     validate each build-option and return the appropriate error, or null if everything is valid
        /// </summary>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        private IActionResult ValidateBuildOptions(ConfigurationBuildOptions buildOptions)
        {
            if (buildOptions is null)
                return BadRequest("no build-options received");

            // if both ValidFrom and ValidTo are set, we make sure that they're valid
            if (!(buildOptions.ValidFrom is null) && !(buildOptions.ValidTo is null))
            {
                if (buildOptions.ValidFrom > buildOptions.ValidTo)
                    return BadRequest($"{nameof(buildOptions.ValidFrom)} can't be later than {nameof(buildOptions.ValidTo)}");

                var minimumActiveTime = TimeSpan.FromMinutes(1.0d);

                if (buildOptions.ValidTo - buildOptions.ValidFrom < minimumActiveTime)
                    return BadRequest("the configuration needs to be valid for at least " +
                                      $"'{minimumActiveTime:g}' ({buildOptions.ValidTo - buildOptions.ValidFrom})");
            }

            return null;
        }
    }
}