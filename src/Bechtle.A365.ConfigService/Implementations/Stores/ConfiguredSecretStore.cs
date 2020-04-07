using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     specific implementation of <see cref="ISecretConfigValueProvider"/> which takes its secrets from the <see cref="IConfigurationSection"/> provided during object-creation
    /// </summary>
    public class ConfiguredSecretStore : ISecretConfigValueProvider
    {
        private readonly IConfigurationSection _configuration;

        /// <inheritdoc cref="ConfiguredSecretStore" />
        public ConfiguredSecretStore(IConfigurationSection configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<IResult<string>> TryGetValue(string path) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public async Task<IResult<Dictionary<string, string>>> TryGetRange(string query) => throw new System.NotImplementedException();
    }
}