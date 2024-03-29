﻿using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     Result of the Compilation of a Config-Structure and a repository of Keys
    /// </summary>
    public class CompilationResult
    {
        /// <inheritdoc cref="CompilationResult" />
        public CompilationResult(IDictionary<string, string?> compiledConfiguration, IEnumerable<TraceResult> traceResults)
        {
            CompiledConfiguration = compiledConfiguration;
            CompilationTrace = traceResults.ToArray();
        }

        /// <summary>
        ///     Traces for each Key that was compiled, starting from the Structure
        /// </summary>
        public TraceResult[] CompilationTrace { get; }

        /// <summary>
        ///     Result of the Compilation
        /// </summary>
        public IDictionary<string, string?> CompiledConfiguration { get; }

        /// <summary>
        ///     Compute and return the List of Keys that were used during this Compilation
        /// </summary>
        /// <returns>List of Keys that were recorded in <see cref="CompilationTrace"/></returns>
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

            var stack = new Stack<TraceResult>();
            foreach (var item in traceResults.GroupBy(r => r.Key)
                                             .Select(g => g.First()))
                stack.Push(item);

            var usedKeys = new List<string>();

            while (stack.TryPop(out var r))
            {
                if (r is KeyTraceResult { Key: { } } k
                    // should be handled  ^^^^^^^^ here,
                    // but resharper wants another check
                    // ReSharper disable once MergeIntoPattern
                    && k.Key is not null)
                {
                    usedKeys.Add(k.Key);
                }

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
