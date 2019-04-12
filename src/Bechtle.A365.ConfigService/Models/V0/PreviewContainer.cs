using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Models.V0
{
    /// <summary>
    ///     Container for all data required to Preview building a Configuration
    /// </summary>
    public class PreviewContainer
    {
        /// <summary>
        ///     Environment-Keys
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }

        /// <summary>
        ///     Structure-Keys
        /// </summary>
        public Dictionary<string, string> Structure { get; set; }

        /// <summary>
        ///     Variable-Keys
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }
}
