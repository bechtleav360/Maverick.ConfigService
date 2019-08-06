using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection
{
    public interface ITracer
    {
        /// <summary>
        ///     list of sub-tracers
        /// </summary>
        IList<ITracer> Children { get; }

        /// <summary>
        ///     add a command to the current tracer
        /// </summary>
        /// <param name="command"></param>
        /// <param name="value"></param>
        void AddCommand(ReferenceCommand command, string value);

        /// <summary>
        ///     create a new sub-tracer representing a path-resolution
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IMultiTracer AddPathResolution(string path);

        /// <summary>
        ///     add a value to the current tracer
        /// </summary>
        /// <param name="value"></param>
        void AddStaticValue(string value);

        /// <summary>
        ///     add a warning-message to the current tracer
        /// </summary>
        /// <param name="warning"></param>
        void AddWarning(string warning);

        /// <summary>
        ///     add a error-message to the current tracer
        /// </summary>
        /// <param name="error"></param>
        void AddError(string error);

        /// <summary>
        ///     get the display-result of this trace until now
        /// </summary>
        /// <returns></returns>
        TraceResult GetResult();
    }
}