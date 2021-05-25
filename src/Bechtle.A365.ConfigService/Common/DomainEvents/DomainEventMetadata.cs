using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public class DomainEventMetadata
    {
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string index]
        {
            get => Filters.TryGetValue(index, out var value) ? value : string.Empty;
            set => Filters[index] = value;
        }
    }
}