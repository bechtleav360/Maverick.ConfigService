using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class KeyTracer : TracerBase
    {
        /// <inheritdoc />
        public KeyTracer(string key, string originalValue)
        {
            Key = key;
            OriginalValue = originalValue;
        }

        public string Key { get; }

        public string OriginalValue { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new KeyTraceResult
        {
            Key = Key,
            OriginalValue = OriginalValue,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}