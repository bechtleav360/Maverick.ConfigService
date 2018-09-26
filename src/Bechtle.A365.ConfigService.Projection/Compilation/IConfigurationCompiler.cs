using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Projection.Compilation
{
    public interface IConfigurationCompiler
    {
        /// <summary>
        ///     compile a big data-set from two separate components
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="structure"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> Compile(IDictionary<string, string> environment,
                                                  IDictionary<string, string> structure,
                                                  IConfigurationParser parser);
    }
}