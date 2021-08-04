namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Configuration for the History of DomainObjects
    /// </summary>
    public class HistoryConfiguration
    {
        /// <summary>
        ///     Controls if new Object-Versions cause older versions to be removed (<see cref="RetainVersions" />)
        /// </summary>
        public bool RemoveOldVersions { get; set; } = true;

        /// <summary>
        ///     Number of Object-Versions to retain
        /// </summary>
        public int RetainVersions { get; set; } = 1;
    }
}
