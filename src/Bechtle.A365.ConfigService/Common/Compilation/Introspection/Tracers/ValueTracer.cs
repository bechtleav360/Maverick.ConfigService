﻿using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    /// <summary>
    ///     Traces the "Compilation" static values
    /// </summary>
    public class ValueTracer : TracerBase
    {
        /// <inheritdoc />
        public ValueTracer(ITracer parent, string staticValue)
            : base(parent)
        {
            StaticValue = staticValue;
        }

        /// <summary>
        ///     Static Value added to the Value of the key being traced
        /// </summary>
        public string StaticValue { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new ValueTraceResult
        {
            Errors = Errors.ToArray(),
            Warnings = Warnings.ToArray(),
            StaticValue = StaticValue,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}