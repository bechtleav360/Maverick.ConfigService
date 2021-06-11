namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     internal representation of an Environment-Key-Action in its 'Initial' version
    /// </summary>
    public struct InitialKeyActionRepr
    {
        public string Key;

        public string Value;

        public string Description;

        public string ValueType;

        public InitialKeyActionTypeRepr Type;
    }
}