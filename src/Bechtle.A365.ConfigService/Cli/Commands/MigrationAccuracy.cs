namespace Bechtle.A365.ConfigService.Cli.Commands
{
    /// <summary>
    ///     Accuracy with which the app should migrate the data
    /// </summary>
    public enum MigrationAccuracy
    {
        /// <summary>
        ///     only the latest relevant state is migrated from one version to another.
        ///     certain Events will be cut from the migrated stream, because it is expected to be recreated (Structures / Configurations).
        /// </summary>
        Lossy,

        /// <summary>
        ///     the complete state will be migrated from one version to another, re-creating all events with equivalent events of the newer version
        /// </summary>
        Lossless
    }
}
