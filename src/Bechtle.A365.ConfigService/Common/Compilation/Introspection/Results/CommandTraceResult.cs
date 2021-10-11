using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <summary>
    ///     Specialized instance of <see cref="TraceResult"/> that keeps track of Commands that were resolved
    /// </summary>
    public class CommandTraceResult : TraceResult
    {
        /// <summary>
        ///     Command whose resolution was traced
        /// </summary>
        public ReferenceCommand Command { get; set; }

        /// <summary>
        ///     Value that was resolved for this Command
        /// </summary>
        public string? Value { get; set; }
    }
}
