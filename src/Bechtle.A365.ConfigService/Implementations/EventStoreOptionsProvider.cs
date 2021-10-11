using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Default implementation of <see cref="IEventStoreOptionsProvider" />, that read the /options/info endpoint from EventStore
    /// </summary>
    public class EventStoreOptionsProvider : IEventStoreOptionsProvider
    {
        private readonly IOptionsSnapshot<EventStoreConnectionConfiguration> _eventStoreConfiguration;
        private readonly List<OptionEntry> _eventStoreOptions;
        private readonly ILogger<EventStoreOptionsProvider> _logger;

        /// <summary>
        ///     indicates if the options have been loaded, so further calls to <see cref="LoadConfiguration" /> can be exited early
        /// </summary>
        private bool _optionsLoaded;

        /// <summary>
        ///     Create a new instance of <see cref="EventStoreOptionsProvider" />
        /// </summary>
        /// <param name="eventStoreConfiguration"></param>
        /// <param name="logger"></param>
        public EventStoreOptionsProvider(
            IOptionsSnapshot<EventStoreConnectionConfiguration> eventStoreConfiguration,
            ILogger<EventStoreOptionsProvider> logger)
        {
            _eventStoreConfiguration = eventStoreConfiguration;
            _logger = logger;
            _eventStoreOptions = new List<OptionEntry>();
            _optionsLoaded = false;
        }

        /// <inheritdoc />
        public bool EventSizeLimited { get; protected set; }

        /// <inheritdoc />
        public async Task LoadConfiguration(CancellationToken cancellationToken)
        {
            if (_optionsLoaded)
            {
                return;
            }

            _eventStoreOptions.Clear();

            List<OptionEntry>? options = await GetEventStoreOptionsAsync(cancellationToken);

            if (options?.Any() == true)
            {
                _optionsLoaded = true;
                _eventStoreOptions.AddRange(options);
            }

            CacheOptions();
        }

        /// <inheritdoc />
        public long MaxEventSizeInBytes { get; protected set; }

        private void CacheOptions()
        {
            EventSizeLimited = _eventStoreOptions.Any(o => o.Name.Equals("MaxAppendSize", StringComparison.OrdinalIgnoreCase));
            MaxEventSizeInBytes =
                EventSizeLimited
                    ? long.Parse(
                        _eventStoreOptions.First(o => o.Name.Equals("MaxAppendSize", StringComparison.OrdinalIgnoreCase))
                                          .Value)
                    : long.MaxValue;
        }

        private async Task<List<OptionEntry>?> GetEventStoreOptionsAsync(CancellationToken cancellationToken = new())
        {
            var storeUri = new Uri(_eventStoreConfiguration.Value.Uri);

            Uri optionsUri = storeUri.Query.Contains("tls=true", StringComparison.OrdinalIgnoreCase)
                                 ? new Uri($"https://{storeUri.Authority}{storeUri.AbsolutePath}info/options")
                                 : new Uri($"http://{storeUri.Authority}{storeUri.AbsolutePath}info/options");

            // yes creating HttpClient is frowned upon, but we don't need it *that* often and can immediately release it
            using var httpClient = new HttpClient();
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(optionsUri, cancellationToken);

                if (response is null)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to read ES-Options");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonConvert.DeserializeObject<List<OptionEntry>>(json);
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "unable to parse ES-Options");
                return null;
            }
        }
    }
}
