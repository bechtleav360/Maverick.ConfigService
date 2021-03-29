using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using EventStore.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations.Health
{
    /// <summary>
    ///     implementation of <see cref="IHealthCheck"/> that tries to read the last event from EventStore, to see if connections are possible
    /// </summary>
    public class EventStoreConnectionCheck : IHealthCheck
    {
        private readonly IOptionsMonitor<EventStoreConnectionConfiguration> _eventStoreConfiguration;

        /// <summary>
        ///     Create a new instance of <see cref="EventStoreConnectionCheck"/>
        /// </summary>
        /// <param name="eventStoreConfiguration">settings on how to connect to the EventStore</param>
        public EventStoreConnectionCheck(IOptionsMonitor<EventStoreConnectionConfiguration> eventStoreConfiguration)
        {
            _eventStoreConfiguration = eventStoreConfiguration;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var settings = EventStoreClientSettings.Create(_eventStoreConfiguration.CurrentValue.Uri);
            settings.ConnectionName = $"ConnectionCheck-{Environment.UserDomainName}\\{Environment.UserName}@{Environment.MachineName}";
            settings.ConnectivitySettings.NodePreference = NodePreference.Follower;
            settings.OperationOptions.TimeoutAfter = TimeSpan.FromMinutes(1);

            using var eventStore = new EventStoreClient(settings);

            try
            {
                await eventStore.ReadAllAsync(Direction.Backwards, Position.End, 1, resolveLinkTos: false, cancellationToken: cancellationToken)
                                .FirstOrDefaultAsync(cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy($"unable to read events from EventStore: {e.Message}");
            }
        }
    }
}