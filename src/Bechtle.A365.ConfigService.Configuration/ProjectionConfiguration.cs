namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Configures how the Projection should behave, and where to connect to
    /// </summary>
    public class ProjectionConfiguration
    {
        /// <inheritdoc cref="EventBusConnectionConfiguration" />
        public EventBusConnectionConfiguration EventBusConnection { get; set; }

        /// <inheritdoc cref="EventStoreConnectionConfiguration" />
        public EventStoreConnectionConfiguration EventStoreConnection { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration" />
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }

        /// <inheritdoc cref="MemoryCacheConfiguration" />
        public MemoryCacheConfiguration MemoryCache { get; set; }
    }
}