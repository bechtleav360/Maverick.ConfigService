using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [Route("structures")]
    public class StructureController : Controller
    {
        private readonly ILogger<StructureController> _logger;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public StructureController(ILogger<StructureController> logger,
                                   IProjectionStore store,
                                   IJsonTranslator translator)
        {
            _logger = logger;
            _store = store;
            _translator = translator;
        }

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableStructures()
        {
            try
            {
                var result = await _store.Structures.GetAvailable();

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve available structures");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve available structures");
            }
        }

        /// <summary>
        ///     get the specified config-structure as list of Key / Value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/keys")]
        public async Task<IActionResult> GetStructureKeys(string name, int version)
        {
            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetKeys(identifier);

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(version)}: {version})");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get the specified config-structure as JSON
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}")]
        public async Task<IActionResult> GetStructure(string name, int version)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest();

            if (version <= 0)
                return BadRequest($"invalid version provided '{version}'");

            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetKeys(identifier);

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to translate structure ({nameof(name)}: {name}, {nameof(version)}: {version}) to JSON");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to translate structure to JSON");
            }
        }

        /// <summary>
        ///     save another version of a named config-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddStructure([FromBody] DtoStructure structure)
        {
            if (structure is null)
                return BadRequest("no Structure received");

            if (string.IsNullOrWhiteSpace(structure.Name))
                return BadRequest($"Structure.{nameof(DtoStructure.Name)} is empty");

            if (structure.Version <= 0)
                return BadRequest($"invalid version provided '{structure.Version}'");

            if (structure.Structure is null)
                return BadRequest($"Structure.{nameof(DtoStructure.Structure)} is empty");

            if (!structure.Structure.Children().Any())
                return BadRequest($"Structure.{nameof(DtoStructure.Structure)} is invalid; does not contain child-nodes");

            try
            {
                var keys = _translator.ToDictionary(structure.Structure);

                return AcceptedAtAction("GetStructureKeys",
                                        new {name = structure.Name, version = structure.Version},
                                        keys);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
                return StatusCode((int) HttpStatusCode.InternalServerError, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
            }
        }
    }
}