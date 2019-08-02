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
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.NonVersioned
{
    /// <summary>
    ///     Query / Manage Environment-Data
    /// </summary>
    [Route("environments")]
    public class EnvironmentController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public EnvironmentController(IServiceProvider provider,
                                     ILogger<EnvironmentController> logger,
                                     IProjectionStore store,
                                     IEventStore eventStore,
                                     IJsonTranslator translator)
            : base(provider, logger)
        {
            _store = store;
            _eventStore = eventStore;
            _translator = translator;
        }

        /// <summary>
        ///     create a new Environment with the given Category + Name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpPost("{category}/{name}", Name = "Deprecated_Fallback_AddEnvironment")]
        public async Task<IActionResult> AddEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var existingEnvs = await _store.Environments.GetAvailableInCategory(category, QueryRange.All);

                if (existingEnvs.IsError)
                    return ProviderError(existingEnvs);

                // if the category doesn't exist at all, add DefaultEnvironment and the requested one
                if (!existingEnvs.Data.Any())
                {
                    // create DefaultEnvironment
                    await new ConfigEnvironment().DefaultIdentifiedBy(category)
                                                 .Create()
                                                 .Save(_eventStore);

                    // create requested environment
                    await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(category, name))
                                                 .Create()
                                                 .Save(_eventStore);
                }
                else
                {
                    // if another Env with that ID exists
                    if (existingEnvs.Data.Any(e => e.Category == category && e.Name == name))
                        return ProviderError(Common.Result.Error($"environment (Category: {category}; Name: {name}) already exists",
                                                                 ErrorCode.EnvironmentAlreadyExists));

                    await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(category, name))
                                                 .Create()
                                                 .Save(_eventStore);
                }

                return AcceptedAtAction(nameof(GetKeys), new {version = ApiVersions.V0, category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to add new environment at ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to add new environment");
            }
        }

        /// <summary>
        ///     delete keys from the environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpDelete("{category}/{name}/keys", Name = "Deprecated_Fallback_DeleteFromEnvironment")]
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
                var actions = keys.Select(ConfigKeyAction.Delete)
                                  .ToArray();

                await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(category, name))
                                             .ModifyKeys(actions)
                                             .Save(_eventStore);

                return AcceptedAtAction(nameof(GetKeys), new {version = ApiVersions.V0, category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to delete keys from Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed delete update keys");
            }
        }

        /// <summary>
        ///     get a list of available environments
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpGet("available", Name = "Deprecated_Fallback_GetAvailableEnvironments")]
        public async Task<IActionResult> GetAvailableEnvironments([FromQuery] int offset = -1,
                                                                  [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);

            var result = await _store.Environments.GetAvailable(range);

            return Result(result);
        }

        /// <summary>
        ///     get the keys contained in an environment
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpGet("{category}/{name}/keys", Name = "Deprecated_Fallback_GetEnvironmentAsKeys")]
        public async Task<IActionResult> GetKeys([FromRoute] string category,
                                                 [FromRoute] string name,
                                                 [FromQuery] string filter,
                                                 [FromQuery] int offset = -1,
                                                 [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);

            IResult<IDictionary<string, string>> result;

            if (string.IsNullOrWhiteSpace(filter))
                result = await _store.Environments.GetKeys(identifier, range);
            else
                result = await _store.Environments.GetKeys(identifier, filter, range);

            return Result(result);
        }

        /// <summary>
        ///     get the keys contained in an environment, converted to JSON
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpGet("{category}/{name}/json", Name = "Deprecated_Fallback_GetEnvironmentAsJson")]
        public async Task<IActionResult> GetKeysAsJson([FromRoute] string category,
                                                       [FromRoute] string name,
                                                       [FromQuery] string filter)
        {
            var identifier = new EnvironmentIdentifier(category, name);

            IResult<IDictionary<string, string>> result;

            if (string.IsNullOrWhiteSpace(filter))
                result = await _store.Environments.GetKeys(identifier, QueryRange.All);
            else
                result = await _store.Environments.GetKeys(identifier, filter, QueryRange.All);

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
        /// <param name="offset" />
        /// <param name="length" />
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpGet("{category}/{name}/keys/objects", Name = "Deprecated_Fallback_GetEnvironmentAsObjects")]
        public async Task<IActionResult> GetKeysWithMetadata([FromRoute] string category,
                                                             [FromRoute] string name,
                                                             [FromQuery] string filter,
                                                             [FromQuery] int offset = -1,
                                                             [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);

            var identifier = new EnvironmentIdentifier(category, name);

            IResult<IEnumerable<DtoConfigKey>> result;

            if (string.IsNullOrWhiteSpace(filter))
                result = await _store.Environments.GetKeyObjects(identifier, range);
            else
                result = await _store.Environments.GetKeyObjects(identifier, filter, range);

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
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpPut("{category}/{name}/keys", Name = "Deprecated_Fallback_UpdateEnvironment")]
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
                var actions = keys.Select(key => ConfigKeyAction.Set(key.Key,
                                                                     key.Value,
                                                                     key.Description,
                                                                     key.Type))
                                  .ToArray();

                await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(category, name))
                                             .ModifyKeys(actions)
                                             .Save(_eventStore);

                return AcceptedAtAction(nameof(GetKeys), new {version = ApiVersions.V0, category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update keys of Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to update keys");
            }
        }
    }
}