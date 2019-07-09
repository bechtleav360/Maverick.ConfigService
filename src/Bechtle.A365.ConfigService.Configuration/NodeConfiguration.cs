using System;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     node-configuration for when multiple instances run at the same time
    /// </summary>
    public class NodeConfiguration
    {
        /// <summary>
        ///     group-identifier between which work is distributed
        ///     only one member of a group may work on an event at the same time
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        ///     how long each incoming event should be locked for processing
        /// </summary>
        public TimeSpan LockDuration { get; set; }
    }
}