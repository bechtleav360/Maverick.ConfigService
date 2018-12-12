using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public abstract class TracerBase : ITracer
    {
        /// <inheritdoc />
        protected TracerBase()
        {
            Children = new List<ITracer>();
        }

        /// <inheritdoc />
        public void AddCommand(ReferenceCommand command, string value) => Children.Add(new CommandTracer(command, value));

        /// <inheritdoc />
        public IMultiTracer AddPathResolution(string path)
        {
            var tracer = new MultiTracer(path);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public void AddStaticValue(string value) => Children.Add(new ValueTracer(value));

        /// <inheritdoc />
        public IList<ITracer> Children { get; }

        /// <inheritdoc />
        public abstract TraceResult GetResult();
    }
}