namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public class DefaultEnvironmentCreated : DomainEvent
    {
        /// <inheritdoc />
        public DefaultEnvironmentCreated(EnvironmentIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public DefaultEnvironmentCreated()
        {
        }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }
    }
}