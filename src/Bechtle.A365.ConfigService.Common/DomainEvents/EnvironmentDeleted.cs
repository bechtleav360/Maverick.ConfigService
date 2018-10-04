namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     an Environment with the given identifier has been deleted
    /// </summary>
    public class EnvironmentDeleted : DomainEvent
    {
        /// <inheritdoc />
        public EnvironmentDeleted(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public EnvironmentDeleted()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }
    }
}