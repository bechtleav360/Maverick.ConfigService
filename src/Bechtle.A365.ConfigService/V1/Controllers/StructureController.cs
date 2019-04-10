using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "structures")]
    public class StructureController : VersionedController<V0.Controllers.StructureController>
    {
        /// <inheritdoc />
        public StructureController(IServiceProvider provider,
                                   ILogger<StructureController> logger,
                                   V0.Controllers.StructureController previousVersion)
            : base(provider, logger, previousVersion)
        {
        }

        /// <summary>
        ///     save another version of a named config-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "AddStructure")]
        public Task<IActionResult> AddStructure([FromBody] DtoStructure structure)
            => PreviousVersion.AddStructure(structure);

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = ApiVersionFormatted + "GetAvailableStructures")]
        public Task<IActionResult> GetAvailableStructures([FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
            => PreviousVersion.GetAvailableStructures(offset, length);

        /// <summary>
        ///     get the specified config-structure as JSON
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpGet("{name}/{version}", Name = ApiVersionFormatted + "GetStructureObsolete")]
        public Task<IActionResult> GetStructure([FromRoute] string name,
                                                [FromRoute] int version,
                                                [FromQuery] int offset = -1,
                                                [FromQuery] int length = -1)
            => PreviousVersion.GetStructure(name, version, offset, length);

        /// <summary>
        ///     get the specified config-structure as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/json", Name = ApiVersionFormatted + "GetStructureAsJson")]
        public Task<IActionResult> GetStructureJson([FromRoute] string name,
                                                    [FromRoute] int version)
            => PreviousVersion.GetStructureJson(name, version);

        /// <summary>
        ///     get the specified config-structure as list of Key / Value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/keys", Name = ApiVersionFormatted + "GetStructureAsKeys")]
        public Task<IActionResult> GetStructureKeys([FromRoute] string name,
                                                    [FromRoute] int version,
                                                    [FromQuery] int offset = -1,
                                                    [FromQuery] int length = -1)
            => PreviousVersion.GetStructureKeys(name, version, offset, length);

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/variables/keys", Name = ApiVersionFormatted + "GetVariablesAsKeys")]
        public Task<IActionResult> GetVariables([FromRoute] string name,
                                                [FromRoute] int version,
                                                [FromQuery] int offset = -1,
                                                [FromQuery] int length = -1)
            => PreviousVersion.GetVariables(name, version, offset, length);

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/variables/json", Name = ApiVersionFormatted + "GetVariablesAsJson")]
        public Task<IActionResult> GetVariablesJson([FromRoute] string name,
                                                    [FromRoute] int version)
            => PreviousVersion.GetVariablesJson(name, version);

        /// <summary>
        ///     remove variables from the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        [HttpDelete("{name}/{version}/variables/keys", Name = ApiVersionFormatted + "DeleteVariablesFromStructure")]
        public Task<IActionResult> RemoveVariables([FromRoute] string name,
                                                   [FromRoute] int version,
                                                   [FromBody] string[] variables)
            => PreviousVersion.RemoveVariables(name, version, variables);

        /// <summary>
        ///     add or update variables for the specified config-structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="changes"></param>
        /// <returns></returns>
        [HttpPut("{name}/{version}/variables/keys", Name = ApiVersionFormatted + "UpdateVariablesInStructure")]
        public Task<IActionResult> UpdateVariables([FromRoute] string name,
                                                   [FromRoute] int version,
                                                   [FromBody] Dictionary<string, string> changes)
            => PreviousVersion.UpdateVariables(name, version, changes);
    }
}