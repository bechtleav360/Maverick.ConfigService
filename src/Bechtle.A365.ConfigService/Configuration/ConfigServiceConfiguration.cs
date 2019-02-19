namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    /// </summary>
    public class ConfigServiceConfiguration
    {
        /// <inheritdoc cref="AuthenticationConfiguration" />
        public AuthenticationConfiguration Authentication { get; set; }

        /// <inheritdoc cref="EventBusConnectionConfiguration" />
        public EventBusConnectionConfiguration EventBusConnection { get; set; }

        /// <inheritdoc cref="EventStoreConnectionConfiguration" />
        public EventStoreConnectionConfiguration EventStoreConnection { get; set; }

        /// <summary>
        /// </summary>
        public string LoggingConfiguration { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration" />
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }

        /// <inheritdoc cref="ProtectedConfiguration" />
        public ProtectedConfiguration Protected { get; set; }
    }
}