using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class ReferenceTracer : TracerBase
    {
        /// <inheritdoc />
        public ReferenceTracer(string path)
        {
            Path = path;
        }

        public string Path { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new ReferenceTraceResult
        {
            Path = Path,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}