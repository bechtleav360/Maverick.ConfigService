using System.Collections.Generic;
using System.Text.Json;

namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     transfer-object containing information about a Configuration-Structure
    /// </summary>
    public class DtoStructure
    {
        /// <summary>
        ///     unique name of this Structure
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     arbitrary json describing the desired configuration once it's built
        /// </summary>
        public JsonElement Structure { get; set; }

        /// <summary>
        ///     additional variables used while building the configuration
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; set; }
    }
}
