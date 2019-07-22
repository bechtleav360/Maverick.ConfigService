namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    ///     status of a DomainEvent within the recorded history
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        ///     event has not been recorded in history
        /// </summary>
        Unknown,

        /// <summary>
        ///     event has been recorded in history
        /// </summary>
        Recorded,

        /// <summary>
        ///     event has been recorded and projected
        /// </summary>
        Projected,

        /// <summary>
        ///     event has been recorded and projected, but newer events overwrite this action
        /// </summary>
        Superseded,
    }
}