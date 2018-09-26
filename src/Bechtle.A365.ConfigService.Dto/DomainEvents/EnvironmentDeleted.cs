namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     an Environment with the given identifier has been deleted
    /// </summary>
    public class EnvironmentDeleted : DomainEvent
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }
    }
}