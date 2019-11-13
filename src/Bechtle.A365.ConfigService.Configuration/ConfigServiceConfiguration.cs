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

        /// <inheritdoc cref="MemoryCacheConfiguration" />
        public MemoryCacheConfiguration MemoryCache { get; set; }

        /// <inheritdoc cref="ProtectedConfiguration" />
        public ProtectedConfiguration Protected { get; set; }
    }
}