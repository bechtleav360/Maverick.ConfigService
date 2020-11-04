using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Query / Manage Environment-Data
    /// </summary>
    [Route(ApiBaseRoute + "environments")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class EnvironmentController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public EnvironmentController(IServiceProvider provider,
                                     ILogger<EnvironmentController> logger,
                                     IProjectionStore store,
                                     IJsonTranslator translator)
            : base(provider, logger)
        {
            _store = store;
            _translator = translator;
        }

        /// <summary>
        ///     create a new Environment with the given Category + Name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost("{category}/{name}", Name = "AddEnvironment")]
        public async Task<IActionResult> AddEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Environments.Create(new EnvironmentIdentifier(category, name), false);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<EnvironmentController>(),
                                        new {version = ApiVersions.V1, category, name});
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to add new environment at ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to add new environment");
            }
        }

        /// <summary>
        ///     delete an existing Environment with the given Category + Name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpDelete("{category}/{name}", Name = "DeleteEnvironment")]
        public async Task<IActionResult> DeleteEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var result = await _store.Environments.Delete(new EnvironmentIdentifier(category, name));
                if (result.IsError)
                    return ProviderError(result);

                return Accepted();
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to delete environment at ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to delete environment");
            }
        }

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetEnvironments")]
        [HttpGet("available", Name = "GetAvailableEnvironments")]
        public async Task<IActionResult> GetAvailableEnvironments([FromQuery] int offset = -1,
                                                                  [FromQuery] int length = -1,
                                                                  [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            try
            {
                var result = await _store.Environments.GetAvailable(range, targetVersion);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve available Environments (" +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve available environments");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="preferExactMatch"></param>
        /// <param name="root"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/keys", Name = "GetEnvironmentAsKeys")]
        public async Task<IActionResult> GetKeys([FromRoute] string category,
                                                 [FromRoute] string name,
                                                 [FromQuery] string filter,
                                                 [FromQuery] string preferExactMatch,
                                                 [FromQuery] string root,
                                                 [FromQuery] int offset = -1,
                                                 [FromQuery] int length = -1,
                                                 [FromQuery] long targetVersion = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);
            try
            {
                var result = await _store.Environments.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = range,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                return result.IsError
                           ? ProviderError(result)
                           : Ok(result.Data.ToImmutableSortedDictionary());
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Environment-Keys (" +
                                   $"{nameof(category)}: {category}; " +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-keys");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment, converted to JSON
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="preferExactMatch"></param>
        /// <param name="root"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/json", Name = "GetEnvironmentAsJson")]
        public async Task<IActionResult> GetKeysAsJson([FromRoute] string category,
                                                       [FromRoute] string name,
                                                       [FromQuery] string filter,
                                                       [FromQuery] string preferExactMatch,
                                                       [FromQuery] string root,
                                                       [FromQuery] long targetVersion = -1)
        {
            try
            {
                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = QueryRange.All,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Environment-Keys (" +
                                   $"{nameof(category)}: {category}; " +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment as json");
            }
        }

        /// <summary>
        ///     get the keys contained in an environment including all their metadata
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="preferExactMatch"></param>
        /// <param name="root"></param>
        /// <param name="offset" />
        /// <param name="length" />
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/keys/objects", Name = "GetEnvironmentAsObjects")]
        public async Task<IActionResult> GetKeysWithMetadata([FromRoute] string category,
                                                             [FromRoute] string name,
                                                             [FromQuery] string filter,
                                                             [FromQuery] string preferExactMatch,
                                                             [FromQuery] string root,
                                                             [FromQuery] int offset = -1,
                                                             [FromQuery] int length = -1,
                                                             [FromQuery] long targetVersion = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var identifier = new EnvironmentIdentifier(category, name);

                var result = await _store.Environments.GetKeyObjects(new KeyQueryParameters<EnvironmentIdentifier>
                {
                    Identifier = identifier,
                    Filter = filter,
                    PreferExactMatch = preferExactMatch,
                    Range = range,
                    RemoveRoot = root,
                    TargetVersion = targetVersion
                });

                if (result.IsError)
                    return ProviderError(result);

                foreach (var item in result.Data)
                {
                    if (item.Description is null)
                        item.Description = string.Empty;
                    if (item.Type is null)
                        item.Type = string.Empty;
                }

                return Ok(result.Data);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to retrieve Environment-Keys (" +
                                   $"{nameof(category)}: {category}; " +
                                   $"{nameof(name)}: {name}; " +
                                   $"{nameof(filter)}: {filter}; " +
                                   $"{nameof(preferExactMatch)}: {preferExactMatch}; " +
                                   $"{nameof(root)}: {root}; " +
                                   $"{nameof(offset)}: {offset}; " +
                                   $"{nameof(length)}: {length}; " +
                                   $"{nameof(targetVersion)}: {targetVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve environment-keys");
            }
        }

        /// <summary>
        ///     assign the given layers in their given order as the content of this Environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="layers">ordered list of Layers to be assigned</param>
        /// <returns></returns>
        [HttpPut("{category}/{name}/layers", Name = "AssignLayers")]
        public async Task<IActionResult> AssignLayers([FromRoute] string category,
                                                      [FromRoute] string name,
                                                      [FromBody] LayerIdentifier[] layers)
        {
            try
            {
                return Result(
                    await _store.Environments.AssignLayers(
                        new EnvironmentIdentifier(category, name),
                        layers));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to assign Layers to Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to assign layers to environment");
            }
        }

        /// <summary>
        ///     get the assigned layers and their order for this Environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{category}/{name}/layers", Name = "GetAssignedLayers")]
        public async Task<IActionResult> GetAssignedLayers([FromRoute] string category,
                                                           [FromRoute] string name)
        {
            try
            {
                return Result(await _store.Environments.GetAssignedLayers(new EnvironmentIdentifier(category, name)));
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, $"failed to retrieve assigned Layers for Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve assigned layers for environment");
            }
        }
    }
}