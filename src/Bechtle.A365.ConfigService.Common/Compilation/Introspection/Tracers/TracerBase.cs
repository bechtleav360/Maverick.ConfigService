using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers
{
    public abstract class TracerBase : ITracer
    {
        /// <inheritdoc />
        protected TracerBase(ITracer parent)
        {
            Parent = parent;
            Children = new List<ITracer>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        /// <summary>
        ///     list of errors associated with this tracer
        /// </summary>
        public IList<string> Errors { get; }

        /// <summary>
        ///     list of warnings associated with this tracer
        /// </summary>
        public IList<string> Warnings { get; }

        /// <inheritdoc />
        public ITracer Parent { get; }

        /// <inheritdoc />
        public void AddCommand(ReferenceCommand command, string value) => Children.Add(new CommandTracer(this, command, value));

        /// <inheritdoc />
        public void AddError(string error) => Errors.Add(error);

        /// <inheritdoc />
        public IMultiTracer AddPathResolution(string path)
        {
            var tracer = new MultiTracer(this, path);
            Children.Add(tracer);

            return tracer;
        }

        /// <inheritdoc />
        public void AddStaticValue(string value) => Children.Add(new ValueTracer(this, value));

        /// <inheritdoc />
        public void AddWarning(string warning) => Warnings.Add(warning);

        /// <inheritdoc />
        public IList<ITracer> Children { get; }

        /// <inheritdoc />
        public abstract TraceResult GetResult();
    }
}