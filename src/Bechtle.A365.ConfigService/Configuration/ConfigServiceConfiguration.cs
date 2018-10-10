using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Extensions;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    /// </summary>
    public class ConfigServiceConfiguration
    {
        private Dictionary<string, EndpointConfiguration> _indexedEndpoints;

        /// <summary>
        ///     Gets or sets Endpoints that can be referenced throughout the rest of the configuration.
        /// </summary>
        public List<EndpointConfiguration> Endpoints { get; set; } = new List<EndpointConfiguration>();

        /// <inheritdoc cref="EventStoreConnectionConfiguration" />
        public EventStoreConnectionConfiguration EventStoreConnection { get; set; }

        /// <summary>
        ///     <see cref="Endpoints" /> sorted into a Dictionary(Name, Endpoint)
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, EndpointConfiguration> IndexedEndpoints
            => _indexedEndpoints ?? (_indexedEndpoints = Endpoints.Where(e => e.IsValid())
                                                                  .ToDictionary(e => e.Name, e => e));

        /// <summary>
        /// </summary>
        public string LoggingConfiguration { get; set; }

        /// <inheritdoc cref="ProjectionStorageConfiguration" />
        public ProjectionStorageConfiguration ProjectionStorage { get; set; }
    }
}