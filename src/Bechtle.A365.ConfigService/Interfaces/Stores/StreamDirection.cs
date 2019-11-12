namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     from which end of the EventStream in which direction should events be streamed
    /// </summary>
    public enum StreamDirection
    {
        /// <summary>
        ///     stream events from the beginning forwards
        /// </summary>
        Forwards,

        /// <summary>
        ///     stream events from the most-current backwards
        /// </summary>
        Backwards
    }
}