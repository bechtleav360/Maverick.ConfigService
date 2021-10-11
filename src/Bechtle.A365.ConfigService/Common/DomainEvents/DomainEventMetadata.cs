using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Additional Metadata written alongside <see cref="DomainEvent" /> to better identify them
    /// </summary>
    public class DomainEventMetadata
    {
        /// <summary>
        ///     List of Filters applicable to the accompanying DomainEvent
        /// </summary>
        public Dictionary<string, string> Filters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Shorthand to get the Value of the a Filter, or <see cref="string.Empty" /> if the Filter doesn't exist
        /// </summary>
        /// <param name="index">Name of a Filter in <see cref="Filters" /></param>
        public string this[string index]
        {
            get => Filters.TryGetValue(index, out var value) ? value : string.Empty;
            set => Filters[index] = value;
        }
    }
}
