using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Projection
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        /// <inheritdoc />
        public async Task<IDictionary<string, string>> Compile(string environmentName, string schema, IConfigurationDatabase database)
        {
        }
    }
}