using System;
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
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     Query / Build Service-Configurations
    /// </summary>
    [Route(ApiBaseRoute + "configurations")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class ConfigurationController : ControllerBase
    {
        private readonly IEventHistoryService _eventHistory;
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;
        private readonly ICommandValidator[] _validators;

        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       IEventStore eventStore,
                                       IProjectionStore store,
                                       IJsonTranslator translator,
                                       IEnumerable<ICommandValidator> validators,
                                       IEventHistoryService eventHistory)
            : base(provider, logger)
        {
            _eventStore = eventStore;
            _store = store;
            _translator = translator;
            _eventHistory = eventHistory;
            _validators = validators.ToArray();
        }

        /// <summary>
        ///     create a new configuration for each combination of given Environment and available structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="buildOptions"></param>
        /// <returns></returns>
        [HttpPost("{environmentCategory}/{environmentName}", Name = "BuildConfigurationsForAllStructures")]
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

            var domainObjects = availableStructures.Data
                                                   .Select(id => new ConfigSnapshot().IdentifiedBy(id, envId)
                                                                                     .ValidFrom(buildOptions?.ValidFrom)
                                                                                     .ValidTo(buildOptions?.ValidTo)
                                                                                     .Create())
                                                   .ToList();

            foreach (var domainObj in domainObjects)
            {
                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                    return BadRequest(errors.Values.SelectMany(_ => _));
            }

            foreach (var domainObj in domainObjects)
                await domainObj.Save(_eventStore, _eventHistory, Logger, Metrics);

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
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}", Name = "BuildConfigurationsForAllVersionsOfStructure")]
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

            var domainObjects = structures.Select(id => new ConfigSnapshot().IdentifiedBy(id, envId)
                                                                            .ValidFrom(buildOptions?.ValidFrom)
                                                                            .ValidTo(buildOptions?.ValidTo)
                                                                            .Create())
                                          .ToList();

            foreach (var domainObj in domainObjects)
            {
                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                    return BadRequest(errors.Values.SelectMany(_ => _));
            }

            foreach (var domainObj in domainObjects)
                await domainObj.Save(_eventStore, _eventHistory, Logger, Metrics);

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
        [HttpPost("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = "BuildConfiguration")]
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

            var domainObj = new ConfigSnapshot().IdentifiedBy(structureId, envId)
                                                .ValidFrom(buildOptions?.ValidFrom)
                                                .ValidTo(buildOptions?.ValidTo)
                                                .Create();

            var errors = domainObj.Validate(_validators);
            if (errors.Any())
                return BadRequest(errors.Values.SelectMany(_ => _));

            await domainObj.Save(_eventStore, _eventHistory, Logger, Metrics);

            return AcceptedAtAction(
                nameof(GetConfiguration),
                RouteUtilities.ControllerName<ConfigurationController>(),
                new
                {
                    version = ApiVersions.V1,
                    environmentCategory = environment.Category,
                    environmentName = environment.Name,
                    structureName = structure.Name,
                    structureVersion = structure.Version,
                    when = DateTime.MinValue,
                    offset = -1,
                    length = -1
                });
        }

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <param name="when"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = "GetAvailableConfigurations")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        [Obsolete]
        public IActionResult GetAvailableConfigurations([FromQuery] DateTime when,
                                                        [FromQuery] int offset = -1,
                                                        [FromQuery] int length = -1)
            => RedirectToActionPermanent(nameof(GetConfigurations),
                                         RouteUtilities.ControllerName<ConfigurationController>(),
                                         new {when, offset, length, version = ApiVersions.V1});

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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/keys", Name = "GetConfigurationKeys")]
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

            var configId = new ConfigurationIdentifier(envIdentifier, structureIdentifier, default);

            var result = await _store.Configurations.GetKeys(configId, when, range);

            if (result.IsError)
                return ProviderError(result);

            var version = await _store.Configurations.GetVersion(configId, when);

            if (version.IsError)
                return ProviderError(version);

            // add version to the response-headers
            Response.Headers.Add("x-version", version.Data);

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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/json", Name = "GetConfigurationJson")]
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

                var configId = new ConfigurationIdentifier(envIdentifier, structureIdentifier, default);

                var result = await _store.Configurations.GetKeys(configId, when, QueryRange.All);

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                if (json is null)
                    return StatusCode(HttpStatusCode.InternalServerError, "failed to translate keys to json");

                var version = await _store.Configurations.GetVersion(configId, when);

                if (version.IsError)
                    return ProviderError(version);

                // add version to the response-headers
                Response.Headers.Add("x-version", version.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "failed to retrieve configuration for (" +
                                   $"{nameof(environmentCategory)}: {environmentCategory}, " +
                                   $"{nameof(environmentName)}: {environmentName}, " +
                                   $"{nameof(structureName)}: {structureName}, " +
                                   $"{nameof(structureVersion)}: {structureVersion}, " +
                                   $"{nameof(when)}: {when:O})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get all available configurations
        /// </summary>
        /// <param name="when"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetConfigurations")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<EnvironmentIdentifier, IList<StructureIdentifier>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigurations([FromQuery] DateTime when,
                                                           [FromQuery] int offset = -1,
                                                           [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var result = await _store.Configurations.GetAvailable(when, range);

            return Result(result);
        }

        /// <summary>
        ///     get configurations whose keys are stale, that should be re-built
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("stale", Name = "GetStaleConfigurations")]
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
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/usedKeys", Name = "GetUsedEnvironmentKeys")]
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

            var result = await _store.Configurations.GetUsedConfigurationKeys(
                             new ConfigurationIdentifier(
                                 envIdentifier,
                                 structureIdentifier,
                                 default),
                             when,
                             range);

            return Result(result);
        }

        /// <summary>
        ///     get the version of the specified configuration
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}/version", Name = "GetConfigurationVersion")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetVersion([FromRoute] string environmentCategory,
                                                    [FromRoute] string environmentName,
                                                    [FromRoute] string structureName,
                                                    [FromRoute] int structureVersion,
                                                    [FromQuery] DateTime when)
        {
            var envIdentifier = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structureIdentifier = new StructureIdentifier(structureName, structureVersion);

            var configId = new ConfigurationIdentifier(envIdentifier, structureIdentifier, default);

            var version = await _store.Configurations.GetVersion(configId, when);

            if (version.IsError)
                return ProviderError(version);

            // add version to the response-headers
            Response.Headers.Add("x-version", version.Data);

            return Result(version);
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