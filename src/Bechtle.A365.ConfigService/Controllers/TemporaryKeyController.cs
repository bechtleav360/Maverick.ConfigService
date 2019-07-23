using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Events;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Services.Stores;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <inheritdoc />
    /// <summary>
    ///     store temporary, high-priority keys to override specific behaviour in other external services (logging, feature-toggles, ...)
    /// </summary>
    [Route(ApiBaseRoute + "temporary")]
    public class TemporaryKeyController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly ITemporaryKeyStore _keyStore;

        /// <inheritdoc />
        public TemporaryKeyController(IServiceProvider provider,
                                      ILogger<TemporaryKeyController> logger,
                                      ITemporaryKeyStore keyStore,
                                      IEventBus eventBus)
            : base(provider, logger)
        {
            _keyStore = keyStore;
            _eventBus = eventBus;
        }

        /// <summary>
        ///     get a specific temporary key for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{structure}/{structureVersion}/{key}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Get([FromRoute] string structure,
                                             [FromRoute] int structureVersion,
                                             [FromRoute] string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure))
                    return BadRequest("structure invalid");

                if (structureVersion < 0)
                    return BadRequest("structureVersion invalid");

                if (string.IsNullOrWhiteSpace(key))
                    return BadRequest("key invalid");

                var result = await _keyStore.Get(MakeTemporaryRegion(structure, structureVersion), key);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not retrieve temporary key due to an internal error");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "could not retrieve temporary keys due to an internal error");
            }
        }

        /// <summary>
        ///     get all available temporary keys for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{structure}/{structureVersion}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> GetAll([FromRoute] string structure,
                                                [FromRoute] int structureVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure))
                    return BadRequest("structure invalid");

                if (structureVersion < 0)
                    return BadRequest("structureVersion invalid");

                var result = await _keyStore.GetAll(MakeTemporaryRegion(structure, structureVersion));

                if (result.Code == ErrorCode.NotFound)
                    return Ok(new Dictionary<string, string>());

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not retrieve all temporary keys in the region due to an internal error");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "could not retrieve all temporary keys in the region due to an internal error");
            }
        }

        /// <summary>
        ///     refresh the lifetime of the targeted key
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpPut("{structure}/{structureVersion}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Refresh([FromRoute] string structure,
                                                 [FromRoute] int structureVersion,
                                                 [FromBody] TemporaryKeyList keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure))
                    return BadRequest("structure invalid");

                if (structureVersion < 0)
                    return BadRequest("structureVersion invalid");

                if (keys is null)
                    return BadRequest("invalid body-data");

                if (keys.Entries is null || !keys.Entries.Any())
                    return BadRequest("no or invalid {Body}.Entries");

                if (keys.Duration == default)
                    return BadRequest("no or invalid {Body}.Duration");

                var result = await _keyStore.Extend(MakeTemporaryRegion(structure, structureVersion),
                                                    keys.Entries.Select(e => e.Key).ToList(),
                                                    keys.Duration);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not refresh temporary keys due to an internal error");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "could not refresh temporary keys due to an internal error");
            }
        }

        /// <summary>
        ///     remove a number of temporary keys for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpDelete("{structure}/{structureVersion}/{key}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Remove([FromRoute] string structure,
                                                [FromRoute] int structureVersion,
                                                [FromBody] string[] keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure))
                    return BadRequest("structure invalid");

                if (structureVersion < 0)
                    return BadRequest("structureVersion invalid");

                if (keys is null || !keys.Any())
                    return BadRequest("no or invalid keys");

                var result = await _keyStore.Remove(MakeTemporaryRegion(structure, structureVersion), keys);

                if (result.IsError)
                    return ProviderError(result);

                await _eventBus.Connect();

                await _eventBus.Publish(new EventMessage
                {
                    Event = new TemporaryKeysExpired
                    {
                        Structure = structure,
                        Version = structureVersion,
                        Keys = keys.ToList()
                    },
                    EventType = nameof(TemporaryKeysExpired)
                });

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not remove temporary keys due to an internal error");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "could not remove temporary keys due to an internal error");
            }
        }

        /// <summary>
        ///     set a list of temporary keys for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpPost("{structure}/{structureVersion}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Set([FromRoute] string structure,
                                             [FromRoute] int structureVersion,
                                             [FromBody] TemporaryKeyList keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure))
                    return BadRequest("structure invalid");

                if (structureVersion < 0)
                    return BadRequest("structureVersion invalid");

                if (keys is null)
                    return BadRequest("invalid body-data");

                if (keys.Entries is null || !keys.Entries.Any())
                    return BadRequest("no or invalid {Body}.Entries");

                if (keys.Duration == default)
                    return BadRequest("no or invalid {Body}.Duration");

                var result = await _keyStore.Set(MakeTemporaryRegion(structure, structureVersion),
                                                 keys.Entries.ToDictionary(e => e.Key, e => e.Value),
                                                 keys.Duration);

                if (result.IsError)
                    return ProviderError(result);

                await _eventBus.Connect();

                await _eventBus.Publish(new EventMessage
                {
                    Event = new TemporaryKeysAdded
                    {
                        Structure = structure,
                        Version = structureVersion,
                        Values = keys.Entries.ToDictionary(e => e.Key, e => e.Value)
                    },
                    EventType = nameof(TemporaryKeysAdded)
                });

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not store temporary keys due to an internal error");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  "could not store temporary key due to an internal error");
            }
        }

        /// <summary>
        ///     format StructureName and StructureVersion in a constant way, to simplify access to <see cref="TemporaryKeyStore" />
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private string MakeTemporaryRegion(string structure, int version) => $"{structure}.{version:D}";
    }
}