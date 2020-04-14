using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     specific implementation of <see cref="ISecretConfigValueProvider"/> which takes its secrets from the <see cref="IConfigurationSection"/> provided during object-creation
    /// </summary>
    public class ConfiguredSecretStore : DictionaryValueProvider, ISecretConfigValueProvider
    {
        /// <inheritdoc cref="ConfiguredSecretStore" />
        public ConfiguredSecretStore(IOptions<ConfiguredSecretStoreConfiguration> configuration)
            : base(configuration.Value.Secrets, "Configured-Secrets")
        {
        }

        /// <inheritdoc />
        Task<IResult<string>> IConfigValueProvider.TryGetValue(string path) => base.TryGetValue(NormalizePath(path));

        /// <inheritdoc />
        Task<IResult<Dictionary<string, string>>> IConfigValueProvider.TryGetRange(string query) => base.TryGetRange(NormalizePath(query));

        private string NormalizePath(string path) => path.Replace('/', ':');
    }
}