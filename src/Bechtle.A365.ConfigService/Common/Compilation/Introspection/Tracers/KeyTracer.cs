using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    /// <summary>
    ///     Tracer for the Compilation of a single Key
    /// </summary>
    public class KeyTracer : TracerBase
    {
        /// <inheritdoc />
        public KeyTracer(ITracer parent,
                         string key,
                         string originalValue)
            : base(parent)
        {
            Key = key;
            OriginalValue = originalValue;
        }

        /// <summary>
        ///     Full Key whose compilation is being traced
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     Original Value of this Key before its compilation is traced
        /// </summary>
        public string OriginalValue { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new KeyTraceResult
        {
            Errors = Errors.ToArray(),
            Warnings = Warnings.ToArray(),
            Key = Key,
            OriginalValue = OriginalValue,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}