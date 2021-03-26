using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Events;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Query / Build Service-Configurations
    /// </summary>
    [Route(ApiBaseRoute + "configurations")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class ConfigurationController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IEventBus _eventBus;

        /// <inheritdoc />
        public ConfigurationController(IServiceProvider provider,
                                       ILogger<ConfigurationController> logger,
                                       IProjectionStore store,
                                       IEventBus eventBus)
            : base(provider, logger)
        {
            _store = store;
            _eventBus = eventBus;
        }

        private bool is_subscribed = false;

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
            if (!is_subscribed)
                await _eventBus.Subscribe<OnConfigurationPublished>((s, message) => { Logger.LogCritical($"{s}::{JsonConvert.SerializeObject(message)}"); });

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

            var structure = availableStructures.Data
                                               .FirstOrDefault(s => s.Name.Equals(structureName) &&
                                                                    s.Version == structureVersion);

            if (structure is null)
                return NotFound($"no versions of structure '{structureName}' found");

            var configId = new ConfigurationIdentifier(environment, structure, 0);

            var stalenessResult = await _store.Configurations.IsStale(configId);

            if (stalenessResult.IsError)
                return ProviderError(stalenessResult);

            bool requestApproved;

            if (stalenessResult.Data)
            {
                var result = await _store.Configurations.Build(configId,
                                                               buildOptions?.ValidFrom,
                                                               buildOptions?.ValidTo);

                if (result.IsError)
                    return ProviderError(result);

                requestApproved = true;
            }
            else
            {
                Logger.LogInformation($"request for new Configuration ({configId}) denied due to it not being stale");
                requestApproved = false;
            }

            // add a header indicating if a new Configuration was actually built or not
            HttpContext.Response.OnStarting(state => Task.FromResult(HttpContext.Response.Headers.TryAdd("x-built", ((bool) state).ToString())),
                                            requestApproved);

            try
            {
                await _eventBus.Publish(new EventMessage
                {
                    Event = new OnConfigurationPublished
                    {
                        EnvironmentCategory = environment.Category,
                        EnvironmentName = environment.Name,
                        StructureName = structure.Name,
                        StructureVersion = structure.Version
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "unable to publish OnConfigurationPublished event");
            }

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
        [Obsolete("use GetConfigurations (GET /) instead")]
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
            try
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
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to add new environment at (" +
                                   $"{nameof(environmentCategory)}: {environmentCategory}; " +
                                   $"{nameof(environmentName)}: {environmentName}; " +
                                   $"{nameof(structureName)}: {structureName}; " +
                                   $"{nameof(structureVersion)}: {structureVersion}; " +
                                   $"{nameof(when)}: {when}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve configuration");
            }
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

                var result = await _store.Configurations.GetJson(configId, when);

                if (result.IsError)
                    return ProviderError(result);

                var json = result.Data;

                if (json.ValueKind == JsonValueKind.Null)
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
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve configuration");
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
            try
            {
                var range = QueryRange.Make(offset, length);
                var result = await _store.Configurations.GetAvailable(when, range);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to add new environment at (" +
                                   $"{nameof(when)}: {when}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve available configurations");
            }
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
            try
            {
                var range = QueryRange.Make(offset, length);
                var result = await _store.Configurations.GetStale(range);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to add new environment at (" +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve stale configurations");
            }
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
            try
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
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to add new environment at (" +
                                   $"{nameof(environmentCategory)}: {environmentCategory}; " +
                                   $"{nameof(environmentName)}: {environmentName}; " +
                                   $"{nameof(structureName)}: {structureName}; " +
                                   $"{nameof(structureVersion)}: {structureVersion}; " +
                                   $"{nameof(when)}: {when}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve used keys in configuration");
            }
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
            try
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
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to add new environment at (" +
                                   $"{nameof(environmentCategory)}: {environmentCategory}; " +
                                   $"{nameof(environmentName)}: {environmentName}; " +
                                   $"{nameof(structureName)}: {structureName}; " +
                                   $"{nameof(structureVersion)}: {structureVersion}; " +
                                   $"{nameof(when)}: {when})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve configuration-version");
            }
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