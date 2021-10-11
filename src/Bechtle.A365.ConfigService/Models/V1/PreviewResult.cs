using System;
using System.Collections.Generic;
using System.Text.Json;

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
        public JsonElement Json { get; set; }

        /// <summary>
        ///     Result as Key->Value Map
        /// </summary>
        public Dictionary<string, string?> Map { get; set; } = new();

        /// <summary>
        ///     List of Environment-Keys used to build the resulting Configuration
        /// </summary>
        public IEnumerable<string> UsedKeys { get; set; } = Array.Empty<string>();
    }
}
