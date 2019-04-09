using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     read existing or create new Config-Structures
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "structures")]
    public class StructureController : ControllerBase
    {
        private readonly V0.StructureController _previousVersion;

        /// <inheritdoc />
        public StructureController(IServiceProvider provider,
                                   ILogger<StructureController> logger,
                                   V0.StructureController previousVersion)
            : base(provider, logger)
        {
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     save another version of a named config-structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "AddStructure")]
        public Task<IActionResult> AddStructure([FromBody] DtoStructure structure)
            => _previousVersion.AddStructure(structure);

        /// <summary>
        ///     get available structures
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        [HttpGet("available", Name = ApiVersionFormatted + "GetAvailableStructures")]
        public Task<IActionResult> GetAvailableStructures([FromQuery] int offset = -1,
                                                          [FromQuery] int length = -1)
            => _previousVersion.GetAvailableStructures(offset, length);

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
            => _previousVersion.GetStructure(name, version, offset, length);

        /// <summary>
        ///     get the specified config-structure as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/json", Name = ApiVersionFormatted + "GetStructureAsJson")]
        public Task<IActionResult> GetStructureJson([FromRoute] string name,
                                                    [FromRoute] int version)
            => _previousVersion.GetStructureJson(name, version);

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
            => _previousVersion.GetStructureKeys(name, version, offset, length);

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
            => _previousVersion.GetVariables(name, version, offset, length);

        /// <summary>
        ///     get all variables for the specified config-structure as key / value pairs
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpGet("{name}/{version}/variables/json", Name = ApiVersionFormatted + "GetVariablesAsJson")]
        public Task<IActionResult> GetVariablesJson([FromRoute] string name,
                                                    [FromRoute] int version)
            => _previousVersion.GetVariablesJson(name, version);

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
            => _previousVersion.RemoveVariables(name, version, variables);

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
            => _previousVersion.UpdateVariables(name, version, changes);
    }
}