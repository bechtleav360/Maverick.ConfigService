using System.Collections.Generic;
using Bechtle.A365.Core.EventBus.Events.Abstraction;

namespace Bechtle.A365.ConfigService.Common.Events
{
    /// <summary>
    ///     event published when temporary high-priority keys have been set
    /// </summary>
    public class TemporaryKeysAdded : IA365Event
    {
        /// <summary>
        ///     name of target structure
        /// </summary>
        public string Structure { get; set; } = string.Empty;

        /// <summary>
        ///     version of target-structure
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        ///     list of values that have been set
        /// </summary>
        public Dictionary<string, string?> Values { get; set; } = new();

        /// <inheritdoc />
        public string EventName => nameof(TemporaryKeysAdded);
    }
}
