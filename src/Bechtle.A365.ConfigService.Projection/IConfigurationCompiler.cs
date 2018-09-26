using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection
{
    public interface IConfigurationCompiler
    {
        /// <summary>
        /// </summary>
        /// <param name="environmentName"></param>
        /// <param name="schema"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> Compile(string environmentName, string schema, IConfigurationDatabase database);
    }
}