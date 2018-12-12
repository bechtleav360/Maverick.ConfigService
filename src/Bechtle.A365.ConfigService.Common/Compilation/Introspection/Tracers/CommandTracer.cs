using System.Linq;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public class CommandTracer : TracerBase
    {
        /// <inheritdoc />
        public CommandTracer(ReferenceCommand command, string value)
        {
            Command = command;
            Value = value;
        }

        public string Value { get; }

        public ReferenceCommand Command { get; }

        /// <inheritdoc />
        public override TraceResult GetResult() => new CommandTraceResult
        {
            Command = Command,
            Value = Value,
            Children = Children.Select(c => c.GetResult())
                               .ToArray()
        };
    }
}