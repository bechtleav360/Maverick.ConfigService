using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class ValueTracer : TracerBase
    {
        /// <inheritdoc />
        public ValueTracer(string staticValue)
        {
            StaticValue = staticValue;
        }

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