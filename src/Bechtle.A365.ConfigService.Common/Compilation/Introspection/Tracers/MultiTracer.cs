using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class MultiTracer : TracerBase, IMultiTracer
    {
        /// <inheritdoc />
        public MultiTracer(string path)
        {
            Path = path;
        }

        public string Path { get; }

        /// <inheritdoc />
        public ITracer AddPathResult(string value)
        {
            var tracer = new KeyTracer(Path, value);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public ITracer AddPathResult(string path, string value)
        {
            var tracer = new KeyTracer(path, value);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public override TraceResult GetResult() => new MultiTraceResult
        {
            Path = Path,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}