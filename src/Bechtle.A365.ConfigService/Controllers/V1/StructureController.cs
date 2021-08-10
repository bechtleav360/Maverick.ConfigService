using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [Route(ApiBaseRoute + "structures")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class StructureController : ControllerBase
    {
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public StructureController(
            ILogger<StructureController> logger,
            IProjectionStore store,
            IJsonTranslator translator)
            : base(logger)
        {
            _store = store;
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

            switch (structure.Structure.ValueKind)
            {
                case JsonValueKind.Object:
                    if (!structure.Structure.EnumerateObject().Any())
                        return BadRequest("empty structure-body given");
                    break;

                case JsonValueKind.Array:
                    if (!structure.Structure.EnumerateArray().Any())
                        return BadRequest("empty structure-body given");
                    break;

                default:
                    return BadRequest("invalid structure-body given (invalid type or null)");
            }

            if (structure.Variables is null || !structure.Variables.Any())
                Logger.LogDebug($"Structure.{nameof(DtoStructure.Variables)} is null or empty, seems fishy but may be correct");

            try
            {
                var keys = _translator.ToDictionary(structure.Structure);
                var variables = (structure.Variables
                                 ?? new Dictionary<string, object>()).ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString());

                var result = await _store.Structures.Create(new StructureIdentifier(structure.Name, structure.Version), keys, variables);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(
                    nameof(GetStructureKeys),
                    RouteUtilities.ControllerName<StructureController>(),
                    new
                    {
                        version = ApiVersions.V1,
                        name = structure.Name,
                        structureVersion = structure.Version,
                        offset = -1,
                        length = -1
                    },
                    keys);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"failed to process given Structure.{nameof(DtoStructure.Structure)}");
            }
        }

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = "GetAvailableStructures")]
        [Obsolete("use GetStructures (GET /) instead")]
        public IActionResult GetAvailableStructures(
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1)
            => RedirectToActionPermanent(
                nameof(GetStructures),
                RouteUtilities.ControllerName<StructureController>(),
                new { offset, length, version = ApiVersions.V1 });

        /// <summary>
        ///     get the specified config-structure as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/json", Name = "GetStructureAsJson")]
        public async Task<IActionResult> GetStructureJson(
            [FromRoute] string name,
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

                var json = _translator.ToJson(result.Data.Items);

                if (json.ValueKind == JsonValueKind.Null)
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
        public async Task<IActionResult> GetStructureKeys(
            [FromRoute] string name,
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
                var result = await _store.Structures.GetKeys(identifier, range);

                return result.IsError
                           ? ProviderError(result)
                           : Ok(result.Data.Items);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode((int)HttpStatusCode.InternalServerError, "failed to retrieve structure");
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
        [HttpGet("{name}/{structureVersion}/keys", Name = "GetStructureAsKeysPaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetStructureKeysPaged(
            [FromRoute] string name,
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
                return Result(await _store.Structures.GetKeys(identifier, range));
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to retrieve structure of ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode((int)HttpStatusCode.InternalServerError, "failed to retrieve structure");
            }
        }

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetStructures")]
        public async Task<IActionResult> GetStructures(
            [FromQuery] int offset = -1,
            [FromQuery] int length = -1)
        {
            try
            {
                var range = QueryRange.Make(offset, length);

                var result = await _store.Structures.GetAvailable(range);

                if (result.IsError)
                    return ProviderError(result);

                var sortedData = result.Data
                                       .Items
                                       .GroupBy(s => s.Name)
                                       .ToDictionary(
                                           g => g.Key,
                                           g => g.Select(s => s.Version)
                                                 .ToArray());

                return Ok(sortedData);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "failed to retrieve available structures");
                return StatusCode((int)HttpStatusCode.InternalServerError, "failed to retrieve available structures");
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
        public async Task<IActionResult> GetVariables(
            [FromRoute] string name,
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

                return result.IsError
                           ? ProviderError(result)
                           : Ok(result.Data.Items);
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
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{structureVersion}/variables/keys", Name = "GetVariablesAsKeysPaged")]
        [ApiVersion(ApiVersions.V11, Deprecated = ApiDeprecation.V11)]
        public async Task<IActionResult> GetVariablesPaged(
            [FromRoute] string name,
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
                return Result(await _store.Structures.GetVariables(identifier, range));
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
        public async Task<IActionResult> GetVariablesJson(
            [FromRoute] string name,
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

                var json = _translator.ToJson(result.Data.Items);

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
        public async Task<IActionResult> RemoveVariables(
            [FromRoute] string name,
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

                var result = await _store.Structures.DeleteVariables(identifier, variables);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(
                    nameof(GetVariables),
                    RouteUtilities.ControllerName<StructureController>(),
                    new { version = ApiVersions.V1, name, structureVersion });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(
                    HttpStatusCode.InternalServerError,
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
        public async Task<IActionResult> UpdateVariables(
            [FromRoute] string name,
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

                var result = await _store.Structures.UpdateVariables(identifier, changes);
                if (result.IsError)
                    return ProviderError(result);

                return AcceptedAtAction(
                    nameof(GetVariables),
                    RouteUtilities.ControllerName<StructureController>(),
                    new { version = ApiVersions.V1, name, structureVersion });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
                return StatusCode(
                    HttpStatusCode.InternalServerError,
                    $"failed to update structure-variables for ({nameof(name)}: {name}, {nameof(structureVersion)}: {structureVersion})");
            }
        }

        /// <summary>
        ///     get all metadata for a Structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="structureVersion"></param>
        /// <returns>Metadata for the Structure</returns>
        [ProducesResponseType(typeof(ConfigStructureMetadata), (int)HttpStatusCode.OK)]
        [HttpGet("{name}/{structureVersion}/info", Name = "GetStructureMetadata")]
        public async Task<IActionResult> GetMetadata(
            [FromRoute] string name,
            [FromRoute] int structureVersion)
        {
            try
            {
                var identifier = new StructureIdentifier(name, structureVersion);
                IResult<ConfigStructureMetadata> result = await _store.Structures.GetMetadata(identifier);

                return Result(result);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(e, "failed to read metadata for structure({Name}, {Version})", name, structureVersion);
                return StatusCode(HttpStatusCode.InternalServerError, "failed to retrieve structure-metadata");
            }
        }
    }
}
