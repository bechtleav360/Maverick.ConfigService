namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     internal representation of an Environment-Key-Action in its 'Initial' version
    /// </summary>
    public struct InitialKeyActionRepr
    {
        /// <summary>
        ///     Full Path (Key) for this Entry
        /// </summary>
        public string Key;

        /// <summary>
        ///     Value for this Entry
        /// </summary>
        public string Value;

        /// <summary>
        ///     Content-Description for this Entry
        /// </summary>
        public string Description;

        /// <summary>
        ///     Content-Type for this Entry
        /// </summary>
        public string ValueType;

        /// <inheritdoc cref="InitialKeyActionTypeRepr"/>/>
        public InitialKeyActionTypeRepr Type;
    }
}