namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     how to connect to the EventStore that should be used
    /// </summary>
    public class EventStoreConnectionConfiguration
    {
        /// <summary>
        ///     stream to which events should be written
        /// </summary>
        public string Stream { get; set; }

        /// <summary>
        ///     Uri with connection-credentials
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        ///     name of this connection
        /// </summary>
        public string ConnectionName { get; set; }
    }
}