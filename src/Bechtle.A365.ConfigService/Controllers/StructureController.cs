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
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;
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
        ///     save another version of a named config-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        [HttpPost(Name = "AddStructure")]
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
                var existingStructures = await _store.Structures.GetAvailableVersions(structure.Name, QueryRange.All);

                if (existingStructures.IsError)
                    return ProviderError(existingStructures);

                // structure has already been submitted and can be viewed at the returned location
                if (existingStructures.Data.Any(v => v == structure.Version))
                    return RedirectToAction(nameof(GetStructureKeys), new {name = structure.Name, version = structure.Version});

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
                Logger.LogError($"failed to process given Structure.{nameof(DtoStructure.Structure)}: {e}");
                return StatusCode((int) HttpStatusCode.InternalServerError, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
            }
        }

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = "GetAvailableStructures")]
        public async Task<IActionResult> GetAvailableStructures([FromQuery] int offset = -1,
                                                                [FromQuery] int length = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var result = await _store.Structures.GetAvailable(range);

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
                Logger.LogError($"failed to retrieve available structures: {e}");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve available structures");
            }
        }

        /// <summary>
        ///     get the specified config-structure as JSON
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpGet("{name}/{version}", Name = "GetStructureObsolete")]
        public async Task<IActionResult> GetStructure([FromRoute] string name,
                                                      [FromRoute] int version,
                                                      [FromQuery] int offset = -1,
                                                      [FromQuery] int length = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest();

            if (version <= 0)
                return BadRequest($"invalid version provided '{version}'");

            var range = QueryRange.Make(offset, length);
            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetKeys(identifier, range);

                if (result.Data?.Any() != true)
                    return Ok(new JObject());

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to translate structure ({nameof(name)}: {name}, {nameof(version)}: {version}) to JSON: {e}");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to translate structure to JSON");
            }
        }

        /// <summary>
        ///     get the specified config-structure as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/json", Name = "GetStructureAsJson")]
        public async Task<IActionResult> GetStructureJson([FromRoute] string name,
                                                          [FromRoute] int version)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (version <= 0)
                return BadRequest($"invalid version provided '{version}'");

            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetKeys(identifier, QueryRange.All);

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                if (json is null)
                    return StatusCode(HttpStatusCode.InternalServerError, "failed to translate keys to json");

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get the specified config-structure as list of Key / Value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/keys", Name = "GetStructureAsKeys")]
        public async Task<IActionResult> GetStructureKeys([FromRoute] string name,
                                                          [FromRoute] int version,
                                                          [FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetKeys(identifier, range);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/variables/keys", Name = "GetVariablesAsKeys")]
        public async Task<IActionResult> GetVariables([FromRoute] string name,
                                                      [FromRoute] int version,
                                                      [FromQuery] int offset = -1,
                                                      [FromQuery] int length = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (version <= 0)
                return BadRequest($"invalid version provided '{version}'");

            var range = QueryRange.Make(offset, length);
            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetVariables(identifier, range);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to retrieve structure-variables of ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/variables/json", Name = "GetVariablesAsJson")]
        public async Task<IActionResult> GetVariablesJson([FromRoute] string name,
                                                          [FromRoute] int version)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (version <= 0)
                return BadRequest($"invalid version provided '{version}'");

            var identifier = new StructureIdentifier(name, version);

            try
            {
                var result = await _store.Structures.GetVariables(identifier, QueryRange.All);

                if (result.IsError)
                    return ProviderError(result);

                var json = _translator.ToJson(result.Data);

                return Ok(json);
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to retrieve structure-variables of ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     remove variables from the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        [HttpDelete("{name}/{version}/variables/keys", Name = "DeleteVariablesFromStructure")]
        public async Task<IActionResult> RemoveVariables([FromRoute] string name,
                                                         [FromRoute] int version,
                                                         [FromBody] string[] variables)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name received");

            if (version <= 0)
                return BadRequest("invalid version version received");

            if (variables is null || !variables.Any())
                return BadRequest("no changes received");

            try
            {
                var identifier = new StructureIdentifier(name, version);

                var actions = variables.Select(ConfigKeyAction.Delete)
                                       .ToArray();

                await new ConfigStructure().IdentifiedBy(identifier)
                                           .ModifyVariables(actions)
                                           .Save(_eventStore);

                return AcceptedAtAction(nameof(GetVariables), new {name, version});
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(version)}: {version})");
            }
        }

        /// <summary>
        ///     add or update variables for the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        [HttpPut("{name}/{version}/variables/keys", Name = "UpdateVariablesInStructure")]
        public async Task<IActionResult> UpdateVariables([FromRoute] string name,
                                                         [FromRoute] int version,
                                                         [FromBody] Dictionary<string, string> changes)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name received");

            if (version <= 0)
                return BadRequest("invalid version version received");

            if (changes is null || !changes.Any())
                return BadRequest("no changes received");

            try
            {
                var identifier = new StructureIdentifier(name, version);

                var actions = changes.Select(kvp => ConfigKeyAction.Set(kvp.Key, kvp.Value))
                                     .ToArray();

                await new ConfigStructure().IdentifiedBy(identifier)
                                           .ModifyVariables(actions)
                                           .Save(_eventStore);

                return AcceptedAtAction(nameof(GetVariables), new {name, version});
            }
            catch (Exception e)
            {
                Logger.LogError($"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(version)}: {version}): {e}");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(version)}: {version})");
            }
        }
    }
}