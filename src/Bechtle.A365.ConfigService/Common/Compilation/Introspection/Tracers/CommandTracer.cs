using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    /// <summary>
    ///     Traces Commands and their resolved Values
    /// </summary>
    public class CommandTracer : TracerBase
    {
        /// <inheritdoc />
        public CommandTracer(ITracer parent,
                             ReferenceCommand command,
                             string value)
            : base(parent)
        {
            Command = command;
            Value = value;
        }

        /// <summary>
        ///     Command whose value is being resolved
        /// </summary>
        public ReferenceCommand Command { get; }

        /// <summary>
        ///     Value resolved for <see cref="Command"/>
        /// </summary>
        public string Value { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new CommandTraceResult
        {
            Errors = Errors.ToArray(),
            Warnings = Warnings.ToArray(),
            Command = Command,
            Value = Value,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}