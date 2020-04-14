namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     base configuration for implementations of Secret-Stores
    /// </summary>
    public abstract class SecretStoreConfiguration
    {
        /// <summary>
        ///     Toggle this Store on / off
        /// </summary>
        public bool Enabled { get; set; }
    }
}