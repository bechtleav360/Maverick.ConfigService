namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Connection-Configuration to read Config-Events from EventBus
    /// </summary>
    public class EventBusConnectionConfiguration
    {
        /// <summary>
        ///     ser URI used to connect to EventBus
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        ///     Hub used to receive Config-Events
        /// </summary>
        public string Hub { get; set; }
    }
}