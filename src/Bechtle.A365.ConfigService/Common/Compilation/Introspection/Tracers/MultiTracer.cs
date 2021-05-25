using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class MultiTracer : TracerBase, IMultiTracer
    {
        /// <inheritdoc />
        public MultiTracer(ITracer parent, string path)
            : base(parent)
        {
            Path = path;
        }

        public string Path { get; }

        /// <inheritdoc />
        public ITracer AddPathResult(string value)
        {
            var tracer = new KeyTracer(this, Path, value);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public ITracer AddPathResult(string path, string value)
        {
            var tracer = new KeyTracer(this, path, value);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public override TraceResult GetResult() => new MultiTraceResult
        {
            Errors = Errors.ToArray(),
            Warnings = Warnings.ToArray(),
            Path = Path,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}