﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    /// </summary>
    [Route(ApiBaseRoute + "environments")]
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

        [HttpPost("{category}/{name}")]
        public async Task<IActionResult> AddEnvironment(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            try
            {
                var existingEnvs = await _store.Environments.GetAvailableInCategory(category);

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

                return AcceptedAtAction(nameof(GetKeys), new {category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to add new environment at ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to add new environment");
            }
        }

        [HttpDelete("{category}/{name}/keys")]
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

                return AcceptedAtAction(nameof(GetKeys), new {category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to delete keys from Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed delete update keys");
            }
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEnvironments()
        {
            var result = await _store.Environments.GetAvailable();

            return Result(result);
        }

        [HttpGet("{category}/{name}/keys")]
        public async Task<IActionResult> GetKeys(string category, string name)
        {
            var identifier = new EnvironmentIdentifier(category, name);

            var result = await _store.Environments.GetKeys(identifier);

            return Result(result);
        }

        [HttpGet("{category}/{name}/json")]
        public async Task<IActionResult> GetKeysAsJson(string category, string name)
        {
            var identifier = new EnvironmentIdentifier(category, name);

            var result = await _store.Environments.GetKeys(identifier);

            if (result.IsError)
                return ProviderError(result);

            var json = _translator.ToJson(result.Data);

            return Ok(json);
        }

        [HttpPut("{category}/{name}/keys")]
        public async Task<IActionResult> UpdateKeys([FromRoute] string category,
                                                    [FromRoute] string name,
                                                    [FromBody] Dictionary<string, string> keys)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest($"{nameof(category)} is empty");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest($"{nameof(name)} is empty");

            if (keys == null || !keys.Any())
                return BadRequest("no keys received");

            try
            {
                var actions = keys.Select(kvp => ConfigKeyAction.Set(kvp.Key, kvp.Value))
                                  .ToArray();

                await new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier(category, name))
                                             .ModifyKeys(actions)
                                             .Save(_eventStore);

                return AcceptedAtAction(nameof(GetKeys), new {category, name});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update keys of Environment ({nameof(category)}: {category}; {nameof(name)}: {name})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to update keys");
            }
        }

        [HttpPut("{category}/{name}/json")]
        public async Task<IActionResult> UpdateKeysFromJson([FromRoute] string category,
                                                            [FromRoute] string name,
                                                            [FromBody] JToken json)
        {
            if (json == null)
                return BadRequest("no keys received");

            // convert given IDictionary to a Dictionary without casting
            var keys = _translator.ToDictionary(json)
                                  .ToDictionary(k => k.Key, k => k.Value);

            return await UpdateKeys(category, name, keys);
        }
    }
}