namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    public class ProjectionEventStoreConfiguration
    {
        public string ConnectionName { get; set; }

        public int MaxLiveQueueSize { get; set; }

        public int ReadBatchSize { get; set; }

        public string SubscriptionName { get; set; }

        public string Uri { get; set; }
    }
}