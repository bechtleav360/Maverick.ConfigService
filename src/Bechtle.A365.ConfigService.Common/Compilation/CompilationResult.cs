using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class CompilationResult
    {
        public IDictionary<string, string> CompiledConfiguration { get; }

        public TraceResult[] CompilationTrace { get; }

        /// <inheritdoc />
        public CompilationResult(IDictionary<string, string> compiledConfiguration, IEnumerable<TraceResult> traceResults)
        {
            CompiledConfiguration = compiledConfiguration;
            CompilationTrace = traceResults.ToArray();
        }
    }
}