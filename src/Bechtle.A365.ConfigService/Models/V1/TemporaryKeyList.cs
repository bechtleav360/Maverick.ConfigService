using System;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     list of temporary keys with a shared duration
    /// </summary>
    public class TemporaryKeyList
    {
        /// <summary>
        ///     How long <see cref="Entries" /> are kept in the store
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        ///     number of temporary entries
        /// </summary>
        public TemporaryKey[] Entries { get; set; }
    }
}