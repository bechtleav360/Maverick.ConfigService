namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     MemoryCache configuration. (used for temporary store)
    /// </summary>
    public class MemoryCacheConfiguration
    {
        /// <inheritdoc cref="RedisCacheConfiguration"/>
        public RedisCacheConfiguration Redis { get; set; }
    }
}