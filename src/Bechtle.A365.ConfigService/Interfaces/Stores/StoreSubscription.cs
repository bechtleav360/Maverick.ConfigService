namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     generic EventStore-Subscription
    /// </summary>
    public struct StoreSubscription
    {
        /// <summary>
        ///     The last event number seen on the subscription
        /// </summary>
        public long? LastEventNumber { get; set; }

        /// <summary>
        ///     The name of the stream to which the subscription is subscribed.
        /// </summary>
        public string StreamId { get; set; }
    }
}