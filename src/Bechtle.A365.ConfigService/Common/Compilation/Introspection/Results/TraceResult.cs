using System;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <summary>
    ///     Result of the Resolution of a Value
    /// </summary>
    public abstract class TraceResult
    {
        /// <summary>
        ///     List of Child-Results that were tracked for this resolution
        /// </summary>
        public TraceResult[] Children { get; set; } = Array.Empty<TraceResult>();

        /// <summary>
        ///     Errors encountered during the resolution of this Value
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Warnings encountered during the resolution of this Value
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }
}
