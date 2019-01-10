namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     type of action to apply to a key
    /// </summary>
    public enum ConfigKeyActionType
    {
        /// <summary>
        ///     add / update the value of the key
        /// </summary>
        Set,

        /// <summary>
        ///     remove the key
        /// </summary>
        Delete
    }
}