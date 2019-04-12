using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Reference to an existing Environment, or custom Keys
    /// </summary>
    public class EnvironmentPreview
    {
        /// <summary>
        ///     Reference to an existing Environment
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     Reference to an existing Environment
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Custom Keys
        /// </summary>
        public Dictionary<string, string> Keys { get; set; }
    }
}