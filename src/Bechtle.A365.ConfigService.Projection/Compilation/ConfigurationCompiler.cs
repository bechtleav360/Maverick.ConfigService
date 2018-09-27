using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Projection.Compilation
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        /// <inheritdoc />
        public Task<IDictionary<string, string>> Compile(IDictionary<string, string> environment,
                                                         IDictionary<string, string> structure,
                                                         IConfigurationParser parser)
            => Task.FromResult((IDictionary<string, string>) new Dictionary<string, string>());
    }
}