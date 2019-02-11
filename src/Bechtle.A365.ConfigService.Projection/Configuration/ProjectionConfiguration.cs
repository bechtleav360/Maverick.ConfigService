namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    public class ProjectionConfiguration
    {
        public ProjectionEventBusConfiguration EventBusConnection { get; set; }

        public ProjectionEventStoreConfiguration EventStoreConnection { get; set; }

        public string LoggingConfiguration { get; set; }

        public ProjectionStorageConfiguration ProjectionStorage { get; set; }
    }
}