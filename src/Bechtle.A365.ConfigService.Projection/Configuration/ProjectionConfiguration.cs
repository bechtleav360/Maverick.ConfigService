namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    public class ProjectionConfiguration
    {
        public ProjectionEventStoreConfiguration EventStore { get; set; }

        public ProjectionStorageConfiguration Storage { get; set; }

        public string LoggingConfiguration { get; set; }
    }
}