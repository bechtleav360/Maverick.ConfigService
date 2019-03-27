namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    /// <summary>
    ///     Configures how the Projection should behave, and where to connect to
    /// </summary>
    public class ProjectionConfiguration
    {
        /// <inheritdoc cref="ProjectionEventBusConfiguration" />
        public ProjectionEventBusConfiguration EventBusConnection { get; set; }

        /// <inheritdoc cref="ProjectionEventStoreConfiguration" />
        public ProjectionEventStoreConfiguration EventStoreConnection { get; set; }

        /// <summary>
        ///     NLog-Config string
        /// </summary>
        public string LoggingConfiguration { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration" />
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }
    }
}