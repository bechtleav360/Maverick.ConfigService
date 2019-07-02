using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Services;
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
        // @TODO: send events out when temporary keys are stored / changed / expired
        private readonly ITemporaryKeyStore _keyStore;

        /// <inheritdoc />
        public TemporaryKeyController(IServiceProvider provider,
                                      ILogger<TemporaryKeyController> logger,
                                      ITemporaryKeyStore keyStore)
            : base(provider, logger)
        {
            _keyStore = keyStore;
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
                                             [FromRoute] string structureVersion,
                                             [FromBody] TemporaryKeyList keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure) ||
                    string.IsNullOrWhiteSpace(structureVersion))
                    return BadRequest("structure / version parameters invalid");

                if (keys is null)
                    return BadRequest("invalid body-data");

                if (keys.Entries is null || !keys.Entries.Any())
                    return BadRequest("no or invalid {Body}.Entries");

                if (keys.Duration == default)
                    return BadRequest("no or invalid {Body}.Duration");

                var result = await _keyStore.Set(MakeTemporaryRegion(structure, structureVersion),
                                                 keys.Entries.ToDictionary(e => e.Key, e => e.Value),
                                                 keys.Duration);

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
        ///     get all available temporary keys for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{structure}/{structureVersion}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> GetAll([FromRoute] string structure,
                                                [FromRoute] string structureVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure) ||
                    string.IsNullOrWhiteSpace(structureVersion))
                    return BadRequest("structure / version parameters invalid");

                var result = await _keyStore.GetAll(MakeTemporaryRegion(structure, structureVersion));

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
        ///     get a specific temporary key for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{structure}/{structureVersion}/{key}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Get([FromRoute] string structure,
                                             [FromRoute] string structureVersion,
                                             [FromRoute] string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure) ||
                    string.IsNullOrWhiteSpace(structureVersion) ||
                    string.IsNullOrWhiteSpace(key))
                    return BadRequest("structure / version parameters invalid");

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
        ///     remove a number of temporary keys for the target-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpDelete("{structure}/{structureVersion}/{key}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Remove([FromRoute] string structure,
                                                [FromRoute] string structureVersion,
                                                [FromBody] string[] keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure) ||
                    string.IsNullOrWhiteSpace(structureVersion))
                    return BadRequest("structure / version parameters invalid");

                if (keys is null || !keys.Any())
                    return BadRequest("no or invalid keys");

                var result = await _keyStore.Remove(MakeTemporaryRegion(structure, structureVersion), keys);

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
        ///     refresh the lifetime of the targeted key
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="structureVersion"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpPut("{structure}/{structureVersion}")]
        [ApiVersion(ApiVersions.V1)]
        public async Task<IActionResult> Refresh([FromRoute] string structure,
                                                 [FromRoute] string structureVersion,
                                                 [FromBody] string[] keys)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(structure) ||
                    string.IsNullOrWhiteSpace(structureVersion))
                    return BadRequest("structure / version parameters invalid");

                if (keys is null || !keys.Any())
                    return BadRequest("no or invalid keys");

                var result = await _keyStore.Extend(MakeTemporaryRegion(structure, structureVersion), keys);

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
        ///     format StructureName and StructureVersion in a constant way, to simplify access to <see cref="TemporaryKeyStore"/>
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private string MakeTemporaryRegion(string structure, string version) => $"{structure}.{version}";
    }
}