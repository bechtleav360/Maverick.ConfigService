using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     component that can resolve lists of <see cref="ConfigValuePart"/> with shared context across parts
    /// </summary>
    public interface IValueResolver
    {
        /// <summary>
        ///     resolve 1..N values from the given parts.
        ///     all returned values will be relative to
        /// <list type="bullet">
        ///     <item>the full path given for direct-references</item>
        ///     <item>the "folder" part for range-references</item>
        /// </list>
        /// </summary>
        /// <param name="path">full path for this entry</param>
        /// <param name="value">raw value of this entry</param>
        /// <param name="tracer">tracer to use for this entry</param>
        /// <param name="parser">some implementation if <see cref="IConfigurationParser"/> to be used to parse the given <paramref name="value"/></param>
        /// <returns></returns>
        Task<IResult<IDictionary<string, string?>>> Resolve(string path, string? value, ITracer tracer, IConfigurationParser parser);
    }
}
