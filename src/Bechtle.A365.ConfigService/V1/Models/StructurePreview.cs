using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.V1.Models
{
    /// <summary>
    ///     Reference to an existing Structure, or custom Keys
    /// </summary>
    public class StructurePreview
    {
        /// <summary>
        ///     Reference to an existing Structure
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Reference to an existing Structure
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     Custom Keys
        /// </summary>
        public Dictionary<string, string> Keys { get; set; }

        /// <summary>
        ///     Custom Variables
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }
}