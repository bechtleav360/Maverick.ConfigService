using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class CompilationTracer : ICompilationTracer
    {
        /// <inheritdoc />
        public CompilationTracer()
        {
            Tracers = new List<ITracer>();
        }

        public List<ITracer> Tracers { get; }

        /// <inheritdoc />
        public ITracer AddKey(string key, string originalValue)
        {
            var tracer = new KeyTracer(key, originalValue);
            Tracers.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public TraceResult[] GetResults() => Tracers.Select(t => t.GetResult())
                                                    .ToArray();
    }
}