namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     type of action to apply to a key
    /// </summary>
    public enum InitialKeyActionTypeRepr
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