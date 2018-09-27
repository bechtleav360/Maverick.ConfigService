namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a number of keys within an Environment have been changed
    /// </summary>
    public class EnvironmentKeysModified : DomainEvent
    {
        /// <inheritdoc />
        public EnvironmentKeysModified(EnvironmentIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public EnvironmentKeysModified()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }
    }
}