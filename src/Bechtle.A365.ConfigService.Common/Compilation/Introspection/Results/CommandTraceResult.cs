using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    /// <inheritdoc />
    public class CommandTraceResult : TraceResult
    {
        public ReferenceCommand Command { get; set; }

        public string Value { get; set; }
    }
}