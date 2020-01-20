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
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
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
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, $"failed to delete environment at ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to delete environment");
            }
        }

        /// <summary>
        ///     delete keys from the environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpDelete("{category}/{name}/keys", Name = "DeleteFromEnvironment")]
        public async Task<IActionResult> DeleteKeys([FromRoute] string category,
                                                    [FromRoute] string name,
                                                    [FromBody] string[] keys)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            if (keys == null || !keys.Any())
                return BadRequest("no keys received");

            try
            {
                var result = await _store.Environments.DeleteKeys(new EnvironmentIdentifier(category, name), keys);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<EnvironmentController>(),
                                        new {version = ApiVersions.V1, category, name});
            }
            catch (Exception e)
            {
                Metrics.Measure.Counter.Increment(KnownMetrics.Exception, e.GetType()?.Name ?? string.Empty);
                Logger.LogError(e, $"failed to delete keys from Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed delete update keys");
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

            var result = await _store.Environments.GetAvailable(range, targetVersion);

            return Result(result);
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

            var result = await _store.Environments.GetKeys(new EnvironmentKeyQueryParameters
            {
                Environment = identifier,
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
            var identifier = new EnvironmentIdentifier(category, name);

            var result = await _store.Environments.GetKeys(new EnvironmentKeyQueryParameters
            {
                Environment = identifier,
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
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);

            var result = await _store.Environments.GetKeyObjects(new EnvironmentKeyQueryParameters
            {
                Environment = identifier,
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

        /// <summary>
        ///     add or update keys in the environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpPut("{category}/{name}/keys", Name = "UpdateEnvironment")]
        public async Task<IActionResult> UpdateKeys([FromRoute] string category,
                                                    [FromRoute] string name,
                                                    [FromBody] DtoConfigKey[] keys)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            if (keys == null || !keys.Any())
                return BadRequest("no keys received");

            var groups = keys.GroupBy(k => k.Key)
                             .ToArray();

            if (groups.Any(g => g.Count() > 1))
                return BadRequest("duplicate keys received: " +
                                  string.Join(';',
                                              groups.Where(g => g.Count() > 1)
                                                    .Select(g => $"'{g.Key}'")));

            try
            {
                var result = await _store.Environments.UpdateKeys(new EnvironmentIdentifier(category, name), keys);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(nameof(GetKeys),
                                        RouteUtilities.ControllerName<EnvironmentController>(),
                                        new {version = ApiVersions.V1, category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update keys of Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to update keys");
            }
        }
    }
}