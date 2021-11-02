using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Background-Service that continuously checks what the latest event in the Config-Stream is.
    ///     This information is pushed to <see cref="ProjectionStatusCheck"/>
    ///     so we know if we're projecting live-events, or still catching up.
    /// </summary>
    public class StreamHeadRetrieverService : BackgroundService
    {
        private readonly IOptionsMonitor<EventStoreConnectionConfiguration> _eventStoreConfiguration;
        private readonly IEventStore _eventStore;
        private readonly ProjectionStatusCheck _projectionStatus;
        private readonly ILogger<StreamHeadRetrieverService> _logger;

        /// <summary>
        ///     Create a new instance of <see cref="StreamHeadRetrieverService"/>
        /// </summary>
        /// <param name="eventStoreConfiguration">
        ///     options-monitor for <see cref="EventStoreConnectionConfiguration"/>
        /// </param>
        /// <param name="eventStore">instance of <see cref="IEventStore"/></param>
        /// <param name="projectionStatus">status-check to update periodically</param>
        /// <param name="logger">logger to write diagnostic information to</param>
        public StreamHeadRetrieverService(
            IOptionsMonitor<EventStoreConnectionConfiguration> eventStoreConfiguration,
            IEventStore eventStore,
            ProjectionStatusCheck projectionStatus,
            ILogger<StreamHeadRetrieverService> logger)
        {
            _eventStoreConfiguration = eventStoreConfiguration;
            _eventStore = eventStore;
            _projectionStatus = projectionStatus;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                EventStoreConnectionConfiguration currentOptions = _eventStoreConfiguration.CurrentValue;

                try
                {
                    StreamedEvent? lastEvent = await _eventStore.ReadEvents(
                                                                    currentOptions.Stream,
                                                                    StreamDirection.Backwards,
                                                                    StreamPosition.End(),
                                                                    1)
                                                                .FirstOrDefaultAsync(stoppingToken);

                    if (lastEvent is null)
                    {
                        _logger.LogWarning(
                            "unable to read events from stream '{Stream}'",
                            currentOptions.Stream);
                    }
                    else
                    {
                        _projectionStatus.SetHeadEvent(lastEvent.Header);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(
                        e,
                        "unable to read head-event from stream '{Stream}'",
                        currentOptions.Stream);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}
