using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Environment-Data used during Compilation as lookup
    /// </summary>
    public class EnvironmentCompilationInfo
    {
        /// <summary>
        ///     Map of Keys/Values
        /// </summary>
        public IDictionary<string, string?> Keys { get; set; } = new Dictionary<string, string?>();

        /// <summary>
        ///     Name of the Environment, that provided the data for <see cref="Keys"/>
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
