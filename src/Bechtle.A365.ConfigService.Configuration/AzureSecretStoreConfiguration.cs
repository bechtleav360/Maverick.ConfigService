using System;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     configuration used to access Secrets from Azure-Key-Vault
    /// </summary>
    public class AzureSecretStoreConfiguration : SecretStoreConfiguration
    {
        /// <summary>
        ///     URI to the Azure-Key-Vault to use
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        ///     ClientId to use when authenticating this application against azure
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        ///     ClientSecret to use when authentication this application against azure
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        ///     TenantId to use when authenticating this application against azure
        /// </summary>
        public string TenantId { get; set; }
    }
}