using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    /// <summary>
    ///     Tracer that keeps track of the resolution of a value with N sub-tracers
    /// </summary>
    public class MultiTracer : TracerBase, IMultiTracer
    {
        /// <inheritdoc />
        public MultiTracer(ITracer parent, string? path)
            : base(parent)
        {
            Path = path;
        }

        /// <summary>
        ///     Full Path to the Key being Traced
        /// </summary>
        public string? Path { get; }

        /// <inheritdoc />
        public ITracer AddPathResult(string? value)
        {
            var tracer = new KeyTracer(this, Path, value);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public ITracer AddPathResult(string path, string? value)
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
