namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <summary>
    ///     specialized instance of <see cref="TraceResult"/> that keeps track of Keys that were resolved
    /// </summary>
    public class KeyTraceResult : TraceResult
    {
        /// <summary>
        ///     Original Key whose value was resolved
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        ///     Original value of <see cref="Key"/> (raw value)
        /// </summary>
        public string? OriginalValue { get; set; }
    }
}
