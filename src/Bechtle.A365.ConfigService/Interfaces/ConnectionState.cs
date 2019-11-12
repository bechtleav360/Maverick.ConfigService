namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     current state of the connection to the EventStore
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        ///     EventStore is connected
        /// </summary>
        Connected,

        /// <summary>
        ///     EventStore is not connected
        /// </summary>
        Disconnected,

        /// <summary>
        ///     EventStore has been disconnected, but is currently trying to reconnect
        /// </summary>
        Reconnecting
    }
}