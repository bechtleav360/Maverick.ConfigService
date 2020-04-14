using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     implementation of <see cref="ISecretConfigValueProvider" /> using Azure Key-Vault Secrets
    /// </summary>
    public class AzureSecretStore : ISecretConfigValueProvider
    {
        private readonly SecretClient _client;
        private readonly ILogger<AzureSecretStore> _logger;

        /// <summary>
        ///     creates a new instance using the keys in the given <paramref name="configuration" />
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AzureSecretStore(IOptions<AzureSecretStoreConfiguration> configuration, ILogger<AzureSecretStore> logger)
        {
            _logger = logger;

            var config = configuration.Value;

            _client = new SecretClient(configuration.Value.Uri,
                                       new ClientSecretCredential(config.TenantId,
                                                                  config.ClientId,
                                                                  config.ClientSecret));
        }

        /// <inheritdoc />
        public Task<IResult<Dictionary<string, string>>> TryGetRange(string query)
            => Task.FromResult(
                Result.Error<Dictionary<string, string>>(
                    nameof(AzureSecretStore) + " does not support Range-Queries",
                    ErrorCode.DbQueryError));

        /// <inheritdoc />
        public async Task<IResult<string>> TryGetValue(string path)
        {
            try
            {
                var response = await _client.GetSecretAsync(path);

                return Result.Success(response.Value.Value);
            }
            catch (RequestFailedException e)
            {
                _logger.LogDebug(e, $"secret '{path}' not found in azure-key-vault");
                return Result.Error<string>($"secret '{path}' not found in azure-key-vault", ErrorCode.DbQueryError);
            }
        }
    }
}