using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     specific implementation of <see cref="ISecretConfigValueProvider"/> which takes its secrets from the <see cref="IConfigurationSection"/> provided during object-creation
    /// </summary>
    public class ConfiguredSecretStore : DictionaryValueProvider, ISecretConfigValueProvider
    {
        /// <inheritdoc cref="ConfiguredSecretStore" />
        public ConfiguredSecretStore(IConfigurationSection configuration)
            : base(ParseSecretConfiguration(configuration), "Configured-Secrets")
        {
        }

        private static Dictionary<string, string> ParseSecretConfiguration(IConfigurationSection configuration)
        {
            var secretSection = "Secrets";

            var pathIndex = (configuration.Path + ":" + secretSection).Length;

            return configuration.GetSection(secretSection)
                                .AsEnumerable()
                                .ToDictionary(kvp => kvp.Key
                                                        .Substring(pathIndex)
                                                        .TrimStart(':'),
                                              kvp => kvp.Value);
        }

        /// <inheritdoc />
        Task<IResult<string>> IConfigValueProvider.TryGetValue(string path) => base.TryGetValue(NormalizePath(path));

        /// <inheritdoc />
        Task<IResult<Dictionary<string, string>>> IConfigValueProvider.TryGetRange(string query) => base.TryGetRange(NormalizePath(query));

        private string NormalizePath(string path) => path.Replace('/', ':');
    }
}