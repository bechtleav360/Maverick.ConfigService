namespace Bechtle.A365.ConfigService.Projection.Configuration
{
    public enum ErrorBehaviour
    {
        /// <summary>
        ///     throw Exceptions and terminate the Application
        /// </summary>
        Stop,

        /// <summary>
        ///     log the Problems and continue the Application
        /// </summary>
        Pause
    }
}