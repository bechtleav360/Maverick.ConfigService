namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     type of storage used to provider values
    /// </summary>
    public enum ConfigValueProviderType
    {
        /// <summary>
        ///     using Keys from the current Environment
        /// </summary>
        Environment,

        /// <summary>
        ///     using Variables from the current Structure
        /// </summary>
        StructVariables,

        /// <summary>
        ///     using Keys from the configured Secret-Store
        /// </summary>
        SecretStore
    }
}