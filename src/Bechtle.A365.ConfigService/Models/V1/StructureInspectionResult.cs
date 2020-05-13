using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Details about the Compilation of a Structure with an Environment
    /// </summary>
    public class StructureInspectionResult
    {
        /// <summary>
        ///     flag indicating if the compilation was successful or not
        /// </summary>
        public bool CompilationSuccessful { get; set; }

        /// <summary>
        ///     resulting compiled configuration
        /// </summary>
        public IDictionary<string, string> CompiledConfiguration { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Path => Error dictionary
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; set; }

        /// <inheritdoc cref="CompilationStats" />
        public CompilationStats Stats { get; set; } = new CompilationStats();

        /// <summary>
        ///     Path => Warning dictionary
        /// </summary>
        public Dictionary<string, List<string>> Warnings { get; set; }

        /// <summary>
        ///     List of Environment-Keys used to compile this Configuration
        /// </summary>
        public List<string> UsedKeys { get; set; }
    }
}