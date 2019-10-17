using System.Collections.Generic;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Dto
{
    /// <summary>
    ///     transfer-object containing information about a Configuration-Structure
    /// </summary>
    public class DtoStructure
    {
        /// <summary>
        ///     unique name of this Structure
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     arbitrary json describing the desired configuration once it's built
        /// </summary>
        public JsonElement Structure { get; set; }

        /// <summary>
        ///     additional variables used while building the configuration
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; set; }
    }
}