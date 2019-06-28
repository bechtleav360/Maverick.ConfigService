namespace Bechtle.A365.ConfigService.Configuration
{
    public class ProjectionEventStoreConfiguration
    {
        /// <summary>
        ///     name of this connection
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        ///     max number of live events in the local queue
        /// </summary>
        public int MaxLiveQueueSize { get; set; }

        /// <summary>
        ///     number of items read per batch
        /// </summary>
        public int ReadBatchSize { get; set; }

        /// <summary>
        ///     stream to which events should be written
        /// </summary>
        public string Stream { get; set; }

        /// <summary>
        ///     Uri with connection-credentials
        /// </summary>
        public string Uri { get; set; }
    }
}