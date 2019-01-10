namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    public class ProjectionConfiguration
    {
        public ProjectionEventBusConfiguration EventBus { get; set; }

        public ProjectionEventStoreConfiguration EventStore { get; set; }

        public string LoggingConfiguration { get; set; }

        public ProjectionStorageConfiguration Storage { get; set; }
    }
}