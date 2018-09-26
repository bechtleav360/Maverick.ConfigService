namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a number of keys within an Environment have been changed
    /// </summary>
    public class EnvironmentKeyModified : DomainEvent
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }

        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }
    }
}