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

        public string[] GetUsedKeys()
        {
            var traceResults = new List<KeyTraceResult>();
            var traceStack = new Stack<TraceResult>();

            foreach (var trace in CompilationTrace)
                traceStack.Push(trace);

            while (traceStack.TryPop(out var trace))
            {
                foreach (var _ in trace.Children)
                    traceStack.Push(_);

                if (trace is KeyTraceResult keyTrace)
                    traceResults.Add(keyTrace);
            }

            var x = traceResults.GroupBy(r => r.Key)
                                .Select(g => g.First())
                                .ToArray();

            var stack = new Stack<TraceResult>();
            foreach (var item in x)
                stack.Push(item);

            var usedKeys = new List<string>();

            while (stack.TryPop(out var r))
            {
                if (r is KeyTraceResult k)
                    usedKeys.Add(k.Key);

                foreach (var c in r.Children)
                    stack.Push(c);
            }

            return usedKeys.GroupBy(_ => _)
                           .Select(_ => _.Key)
                           .OrderBy(_ => _)
                           .ToArray();
        }
    }
}