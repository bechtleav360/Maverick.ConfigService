using System;
using Bechtle.A365.Core.EventBus.Events.Messages;

namespace Bechtle.A365.ConfigService.Projection.EventBusMessages
{
    /// <summary>
    ///     Sent by ConfigService.Projection to indicate that a new Configuration was built
    ///     and can be retrieved in the period between <see cref="ValidFrom" /> and <see cref="ValidTo" />
    /// </summary>
    public class OnVersionBuilt : EventMessage
    {
        /// <inheritdoc />
        public OnVersionBuilt()
        {
            Id = Guid.NewGuid();
            ClientId = nameof(OnVersionBuilt);
        }

        /// <summary>
        ///     Environment Category + Name $"{Category}/{Name}"
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        ///     Structure Name
        /// </summary>
        public string Structure { get; set; }

        /// <summary>
        ///     Structure Version
        /// </summary>
        public int StructureVersion { get; set; }

        /// <summary>
        ///     Configuration is valid from this point in time, or always if null
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        ///     Configuration is valid up to this point in time, or indefinitely if null
        /// </summary>
        public DateTime? ValidTo { get; set; }
    }
}