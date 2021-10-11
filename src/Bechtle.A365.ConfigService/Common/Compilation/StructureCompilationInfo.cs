using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Structure-Data used as blueprint to compile a Configuration
    /// </summary>
    public class StructureCompilationInfo
    {
        /// <summary>
        ///     Keys acting as Roots for the Compilation
        /// </summary>
        public IDictionary<string, string?> Keys { get; set; } = new Dictionary<string, string?>();

        /// <summary>
        ///     Name of the Structure that provided <see cref="Keys"/> and <see cref="Variables"/>
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Additional Variables usable during the Compilation
        /// </summary>
        public IDictionary<string, string?> Variables { get; set; } = new Dictionary<string, string?>();
    }
}
