using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    /// <summary>
    ///     base-implementation of <see cref="ICompilationTracer"/>
    /// </summary>
    public class CompilationTracer : ICompilationTracer
    {
        /// <inheritdoc cref="CompilationTracer" />
        public CompilationTracer()
        {
            Tracers = new List<ITracer>();
        }

        /// <summary>
        ///     Root-Tracers registered for this Compilation
        /// </summary>
        public List<ITracer> Tracers { get; }

        /// <inheritdoc />
        public ITracer AddKey(string key, string originalValue)
        {
            var tracer = new KeyTracer(null, key, originalValue);
            Tracers.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public TraceResult[] GetResults() => Tracers.Where(t => !(t is null))
                                                    .Select(t => t.GetResult())
                                                    .ToArray();
    }
}