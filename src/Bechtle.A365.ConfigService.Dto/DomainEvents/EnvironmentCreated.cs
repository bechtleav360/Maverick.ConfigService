namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     an Environment has been created under the given identifier
    /// </summary>
    public class EnvironmentCreated : DomainEvent
    {
        /// <inheritdoc />
        public EnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public EnvironmentCreated()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }
    }
}