namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <summary>
    ///     specialized instance of <see cref="TraceResult"/> that keeps track of static values being "resolved"
    /// </summary>
    public class ValueTraceResult : TraceResult
    {
        /// <summary>
        ///     Static value resolved as value for a Key
        /// </summary>
        public string StaticValue { get; set; } = string.Empty;
    }
}
