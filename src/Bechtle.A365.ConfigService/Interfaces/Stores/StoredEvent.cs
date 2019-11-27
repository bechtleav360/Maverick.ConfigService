using System;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    /// <summary>
    ///     generic Event stored in an EventStore
    /// </summary>
    public struct StoredEvent
    {
        /// <summary>
        ///     A byte array representing the data of this event
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        ///     A byte array representing the metadata associated with this event
        /// </summary>
        public byte[] Metadata { get; set; }

        /// <summary>
        ///     A datetime representing when this event was created in the system
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        ///     The number of this event in the stream
        /// </summary>
        public long EventNumber { get; set; }

        /// <summary>
        ///     The Unique Identifier representing this event
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        ///     The type of event this is
        /// </summary>
        public string EventType { get; set; }
    }
}