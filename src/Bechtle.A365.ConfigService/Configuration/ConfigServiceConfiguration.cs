namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    /// </summary>
    public class ConfigServiceConfiguration
    {
        /// <inheritdoc cref="EventStoreConnectionConfiguration"/>
        public EventStoreConnectionConfiguration EventStoreConnection { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration"/>
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }

        /// <summary>
        /// </summary>
        public string LoggingConfiguration { get; set; }
    }
}