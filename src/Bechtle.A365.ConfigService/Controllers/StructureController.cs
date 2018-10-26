using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [Route(ApiBaseRoute + "structures")]
    public class StructureController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IEventStore _eventStore;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public StructureController(IServiceProvider provider,
                                   ILogger<StructureController> logger,
                                   IProjectionStore store,
                                   IEventStore eventStore,
                                   IJsonTranslator translator) : base(provider, logger)
        {
            _store = store;
            _eventStore = eventStore;
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

                if (result.IsError)
                    return ProviderError(result);

                var sortedData = result.Data
                                       .GroupBy(s => s.Name)
                                       .ToDictionary(g => g.Key, g => g.Select(s => s.Version)
                                                                       .ToArray());

                return Ok(sortedData);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "failed to retrieve available structures");
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

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(version)}: {version})");
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

                if (result.Data?.Any() != true)
                    return Ok(new JObject());

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to translate structure ({nameof(name)}: {name}, {nameof(version)}: {version}) to JSON");
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

            if (structure.Variables is null || !structure.Variables.Any())
                Logger.LogDebug($"Structure.{nameof(DtoStructure.Variables)} is null or empty, seems fishy but may be correct");

            try
            {
                var existingStructures = await _store.Structures.GetAvailableVersions(structure.Name);

                if (existingStructures.IsError)
                    return ProviderError(existingStructures);

                // structure has already been submitted and can be viewed at the returned location
                if (existingStructures.Data.Any(v => v == structure.Version))
                    return RedirectToAction(nameof(GetStructureKeys), 
                                            new {name = structure.Name, version = structure.Version});

                var keys = _translator.ToDictionary(structure.Structure);
                var variables = structure.Variables ?? new Dictionary<string, string>();

                await new ConfigStructure().IdentifiedBy(new StructureIdentifier(structure.Name, structure.Version))
                                           .Create(keys, variables)
                                           .Save(_eventStore);

                return AcceptedAtAction(nameof(GetStructureKeys),
                                        new {name = structure.Name, version = structure.Version},
                                        keys);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
                return StatusCode((int) HttpStatusCode.InternalServerError, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
            }
        }
    }
}