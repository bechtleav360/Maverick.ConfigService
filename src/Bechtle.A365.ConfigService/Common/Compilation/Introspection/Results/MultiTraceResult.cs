namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <summary>
    ///     specialized instance of <see cref="TraceResult"/> that tracks the resolution of multiple values for a given path
    /// </summary>
    public class MultiTraceResult : TraceResult
    {
        /// <summary>
        ///     Path that was resolved to multiple values
        /// </summary>
        public string Path { get; set; }
    }
}