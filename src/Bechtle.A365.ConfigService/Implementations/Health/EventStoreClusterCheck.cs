using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     implementation of <see cref="IHealthCheck" /> that checks
    ///     if the service is connected to the EventStore correctly (discover+cluster||node+single-node)
    /// </summary>
    public class EventStoreClusterCheck : IHealthCheck
    {
        private readonly HttpClient _client;
        private readonly IServiceProvider _provider;

        /// <summary>
        ///     creates a new instance of this health-check
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="clientFactory"></param>
        public EventStoreClusterCheck(IServiceProvider provider, IHttpClientFactory clientFactory)
        {
            _provider = provider;
            _client = clientFactory.CreateClient();
            _client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var config = _provider.GetRequiredService<IOptionsMonitor<EventStoreConnectionConfiguration>>();
            var storeUri = new Uri(config.CurrentValue.Uri);

            var optionsUri = storeUri.Query.Contains("tls=true", StringComparison.OrdinalIgnoreCase)
                                 ? new Uri($"https://{storeUri.Authority}{storeUri.AbsolutePath}info/options")
                                 : new Uri($"http://{storeUri.Authority}{storeUri.AbsolutePath}info/options");

            var response = await _client.GetAsync(optionsUri, cancellationToken);

            if (response is null)
                return HealthCheckResult.Degraded("unable to retrieve [EventStore]/info/options");

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var options = JsonConvert.DeserializeObject<List<OptionEntry>>(json);

                var clusterSizeOption = options?.FirstOrDefault(o => o.Name.Equals("ClusterSize", StringComparison.OrdinalIgnoreCase));

                if (clusterSizeOption is null)
                    return HealthCheckResult.Unhealthy("unable to find 'ClusterSize' in [EventStore]/info/options response");

                if (!long.TryParse(clusterSizeOption.Value, out var clusterSize))
                    return HealthCheckResult.Unhealthy($"unable to read '{clusterSizeOption.Value}' as numeric cluster-size");

                // this is what we're actually interested in
                // single-node, with cluster-connection
                if (clusterSize <= 1 && storeUri.Scheme.Equals("esdb+discover", StringComparison.OrdinalIgnoreCase))
                    return HealthCheckResult.Unhealthy("connected to EventStore-Node in Cluster-mode");

                // single-node, with direct-connection
                if (clusterSize <= 1 && storeUri.Scheme.Equals("esdb", StringComparison.OrdinalIgnoreCase))
                    return HealthCheckResult.Healthy("directly connected to EventStore-Node");

                // cluster, with cluster-connection
                if (clusterSize >= 2 && storeUri.Scheme.Equals("esdb+discover", StringComparison.OrdinalIgnoreCase))
                    return HealthCheckResult.Healthy("connected to EventStore-Cluster in Cluster-Mode");

                // cluster, with direct-connection
                if (clusterSize >= 2 && storeUri.Scheme.Equals("esdb", StringComparison.OrdinalIgnoreCase))
                    return HealthCheckResult.Unhealthy("directly connected to Node in EventStore-Cluster");
            }
            catch (JsonException e)
            {
                return HealthCheckResult.Unhealthy("unable to read response of [EventStore]/info/options", e);
            }

            return HealthCheckResult.Unhealthy();
        }
    }
}