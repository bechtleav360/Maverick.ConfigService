﻿using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.Core.EventBus.Events.Abstraction;

namespace Bechtle.A365.ConfigService.Common.Events
{
    /// <summary>
    ///     event published when temporary high-priority keys have expired
    /// </summary>
    public class TemporaryKeysExpired : IA365Event
    {
        /// <summary>
        ///     list of keys that have expired
        /// </summary>
        public List<string> Keys { get; set; }

        /// <summary>
        ///     name of target structure
        /// </summary>
        public string Structure { get; set; }

        /// <summary>
        ///     version of target-structure
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        ///     Creates a new instance of <see cref="TemporaryKeysExpired" />
        /// </summary>
        /// <param name="structure">structure, for which the temp-keys expired</param>
        /// <param name="version">version of the structure</param>
        /// <param name="keys">keys that have expired</param>
        public TemporaryKeysExpired(string structure, int version, IEnumerable<string> keys)
        {
            Structure = structure;
            Version = version;
            Keys = keys.ToList();
        }

        /// <inheritdoc />
        public string EventName => nameof(TemporaryKeysExpired);
    }
}
