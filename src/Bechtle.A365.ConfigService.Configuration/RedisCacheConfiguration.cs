namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Redis-Cache configuration
    /// </summary>
    public class RedisCacheConfiguration
    {
        /// <summary>
        ///     ConnectionString passed to StackExchange.Redis client
        /// </summary>
        public string ConnectionString { get; set; }
    }
}