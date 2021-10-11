using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     config-based Secret-Store configuration
    /// </summary>
    public class ConfiguredSecretStoreConfiguration : SecretStoreConfiguration
    {
        /// <summary>
        ///     dictionary of all available Secrets.
        ///     Keys represent the whole path, can be separated using ':' or '/'
        /// </summary>
        public Dictionary<string, string> Secrets { get; set; } = new();
    }
}
