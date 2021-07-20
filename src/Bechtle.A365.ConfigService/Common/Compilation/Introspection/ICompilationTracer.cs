using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection
{
    /// <summary>
    ///     Component that traces the Keys and references used to compile a Configuration
    /// </summary>
    public interface ICompilationTracer
    {
        /// <summary>
        ///     create a new tracer for the given key, and recording the original value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="originalValue"></param>
        /// <returns></returns>
        ITracer AddKey(string key, string originalValue);

        /// <summary>
        ///     get the collected traces as a single result
        /// </summary>
        /// <returns></returns>
        TraceResult[] GetResults();
    }
}