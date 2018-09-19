namespace Bechtle.A365.ConfigService.Projection
{
    public class ProjectionConfiguration
    {
        public string ConnectionName { get; set; }

        public string EventStoreUri { get; set; }

        public int MaxLiveQueueSize { get; set; }

        public int ReadBatchSize { get; set; }

        public string SubscriptionName { get; set; }
    }
}