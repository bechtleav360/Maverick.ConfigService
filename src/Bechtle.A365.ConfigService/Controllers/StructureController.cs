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
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [Route(ApiBaseRoute + "structures")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class StructureController : ControllerBase
    {
        private readonly IEventHistoryService _eventHistory;
        private readonly IEventStore _eventStore;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;
        private readonly ICommandValidator[] _validators;

        /// <inheritdoc />
        public StructureController(IServiceProvider provider,
                                   ILogger<StructureController> logger,
                                   IProjectionStore store,
                                   IEventStore eventStore,
                                   IJsonTranslator translator,
                                   IEnumerable<ICommandValidator> validators,
                                   IEventHistoryService eventHistory)
            : base(provider, logger)
        {
            _store = store;
            _eventStore = eventStore;
            _translator = translator;
            _eventHistory = eventHistory;
            _validators = validators.ToArray();
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
                var keys = _translator.ToDictionary(structure.Structure);
                var variables = structure.Variables ?? new Dictionary<string, string>();

                var domainObj = new ConfigStructure().IdentifiedBy(new StructureIdentifier(structure.Name, structure.Version))
                                                     .Create(keys, variables);

                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                    return BadRequest(errors.Values.SelectMany(_ => _));

                await domainObj.Save(_eventStore, _eventHistory, Logger);

                return AcceptedAtAction(nameof(GetStructureKeys),
                                        RouteUtilities.ControllerName<StructureController>(),
                                        new {version = "1.0", name = structure.Name, structureVersion = structure.Version},
                                        keys);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
                return StatusCode((int) HttpStatusCode.InternalServerError, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
            }
        }

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetStructures")]
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
                Logger.LogError(e, "failed to retrieve available structures");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve available structures");
            }
        }

        /// <summary>
        ///     get the specified config-structure as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/json", Name = "GetStructureAsJson")]
        public async Task<IActionResult> GetStructureJson([FromRoute] string name,
                                                          [FromRoute] int structureVersion)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (structureVersion <= 0)
                return BadRequest($"invalid version provided '{structureVersion}'");

            var identifier = new StructureIdentifier(name, structureVersion);

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
                Logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get the specified config-structure as list of Key / Value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/keys", Name = "GetStructureAsKeys")]
        public async Task<IActionResult> GetStructureKeys([FromRoute] string name,
                                                          [FromRoute] int structureVersion,
                                                          [FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
        {
            var range = QueryRange.Make(offset, length);
            var identifier = new StructureIdentifier(name, structureVersion);

            try
            {
                var result = await _store.Structures.GetKeys(identifier, range);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode((int) HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/variables/keys", Name = "GetVariablesAsKeys")]
        public async Task<IActionResult> GetVariables([FromRoute] string name,
                                                      [FromRoute] int structureVersion,
                                                      [FromQuery] int offset = -1,
                                                      [FromQuery] int length = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (structureVersion <= 0)
                return BadRequest($"invalid version provided '{structureVersion}'");

            var range = QueryRange.Make(offset, length);
            var identifier = new StructureIdentifier(name, structureVersion);

            try
            {
                var result = await _store.Structures.GetVariables(identifier, range);

                return Result(result);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to retrieve structure-variables of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/variables/json", Name = "GetVariablesAsJson")]
        public async Task<IActionResult> GetVariablesJson([FromRoute] string name,
                                                          [FromRoute] int structureVersion)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name provided");

            if (structureVersion <= 0)
                return BadRequest($"invalid version provided '{structureVersion}'");

            var identifier = new StructureIdentifier(name, structureVersion);

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
                Logger.LogError(e, $"failed to retrieve structure-variables of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     remove variables from the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        [HttpDelete("{name}/{structureVersion}/variables/keys", Name = "DeleteVariablesFromStructure")]
        public async Task<IActionResult> RemoveVariables([FromRoute] string name,
                                                         [FromRoute] int structureVersion,
                                                         [FromBody] string[] variables)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name received");

            if (structureVersion <= 0)
                return BadRequest("invalid version version received");

            if (variables is null || !variables.Any())
                return BadRequest("no changes received");

            try
            {
                var identifier = new StructureIdentifier(name, structureVersion);

                var actions = variables.Select(ConfigKeyAction.Delete)
                                       .ToArray();

                var domainObj = new ConfigStructure().IdentifiedBy(identifier)
                                                     .ModifyVariables(actions);

                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                    return BadRequest(errors.Values.SelectMany(_ => _));

                await domainObj.Save(_eventStore, _eventHistory, Logger);

                return AcceptedAtAction(nameof(GetVariables), new {name, structureVersion});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
            }
        }

        /// <summary>
        ///     add or update variables for the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        [HttpPut("{name}/{structureVersion}/variables/keys", Name = "UpdateVariablesInStructure")]
        public async Task<IActionResult> UpdateVariables([FromRoute] string name,
                                                         [FromRoute] int structureVersion,
                                                         [FromBody] Dictionary<string, string> changes)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("no name received");

            if (structureVersion <= 0)
                return BadRequest("invalid version version received");

            if (changes is null || !changes.Any())
                return BadRequest("no changes received");

            try
            {
                var identifier = new StructureIdentifier(name, structureVersion);

                var actions = changes.Select(kvp => ConfigKeyAction.Set(kvp.Key, kvp.Value))
                                     .ToArray();

                var domainObj = new ConfigStructure().IdentifiedBy(identifier)
                                                     .ModifyVariables(actions);

                var errors = domainObj.Validate(_validators);
                if (errors.Any())
                    return BadRequest(errors.Values.SelectMany(_ => _));

                await domainObj.Save(_eventStore, _eventHistory, Logger);

                return AcceptedAtAction(nameof(GetVariables), new {name, structureVersion});
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(HttpStatusCode.InternalServerError,
                                  $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
            }
        }
    }
}