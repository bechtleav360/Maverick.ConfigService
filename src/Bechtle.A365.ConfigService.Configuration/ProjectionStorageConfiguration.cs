namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     how to connect to the Database containing the projected configurations
    /// </summary>
    public class ProjectionStorageConfiguration
    {
        /// <summary>
        ///     connection-string to projection database
        /// </summary>
        public string ConnectionString { get; set; }
    }
}