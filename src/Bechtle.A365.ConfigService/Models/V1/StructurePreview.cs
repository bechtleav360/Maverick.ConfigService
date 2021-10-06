using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Reference to an existing Structure, or custom Keys
    /// </summary>
    public class StructurePreview
    {
        /// <summary>
        ///     Custom Keys
        /// </summary>
        public Dictionary<string, object> Keys { get; set; } = new();

        /// <summary>
        ///     Reference to an existing Structure
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Custom Variables
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();

        /// <summary>
        ///     Reference to an existing Structure
        /// </summary>
        public string Version { get; set; } = string.Empty;
    }
}
