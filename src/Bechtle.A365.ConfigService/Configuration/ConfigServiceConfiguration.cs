namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Main Configuration for this Service
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
        ///     NLog Configuration in XML-Format
        /// </summary>
        public string LoggingConfiguration { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration" />
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }

        /// <inheritdoc cref="ProtectedConfiguration" />
        public ProtectedConfiguration Protected { get; set; }
    }
}