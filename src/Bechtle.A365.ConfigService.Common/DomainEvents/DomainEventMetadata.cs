using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public class DomainEventMetadata
    {
        public Dictionary<string, string> Filters { get; set; }

        public string this[string index]
        {
            get
            {
                // @TODO: remove var-init when VS finally understands that var is assigned after the condition is evaluated (out var value)
                var value = string.Empty;
                return Filters?.TryGetValue(index, out value) == true ? value : string.Empty;
            }
            set => Filters[index] = value;
        }
    }
}