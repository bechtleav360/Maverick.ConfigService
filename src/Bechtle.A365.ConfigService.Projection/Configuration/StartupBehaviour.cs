namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    /// <summary>
    ///     defines how the service should react when starting
    /// </summary>
    public class StartupBehaviour
    {
        /// <summary>
        ///     what to do when the Projection encounters problems during Startup
        /// </summary>
        public ErrorBehaviour OnError { get; set; }
    }
}