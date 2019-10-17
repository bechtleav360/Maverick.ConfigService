using System.Collections.Generic;


namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Result of a Preview-Build
    /// </summary>
    public class PreviewResult
    {
        /// <summary>
        ///     Result as JSON
        /// </summary>
        public JToken Json { get; set; }

        /// <summary>
        ///     Result as Key->Value Map
        /// </summary>
        public IDictionary<string, string> Map { get; set; }

        /// <summary>
        ///     List of Environment-Keys used to build the resulting Configuration
        /// </summary>
        public IEnumerable<string> UsedKeys { get; set; }
    }
}