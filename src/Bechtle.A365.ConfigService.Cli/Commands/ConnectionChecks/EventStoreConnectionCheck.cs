using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using EventStore.Client;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class EventStoreConnectionCheck : IConnectionCheck
    {
        /// <summary>
        ///     number of events counted in the target stream
        /// </summary>
        private int _countedEvents;

        private IOutput _output;

        /// <summary>
        ///     flag indicating if the Subscription has been dropped before finishing the event-count - see <see cref="_countedEvents" />
        /// </summary>
        private bool _subscriptionDropped;

        /// <inheritdoc />
        public async Task<TestResult> Execute(IOutput output, TestParameters parameters, ApplicationSettings settings)
        {
            _output = output;

            output.WriteLine("Connecting to EventStore using Effective Configuration");

            var configuration = settings.EffectiveConfiguration.GetSection("EventStoreConnection").Get<EventStoreConnectionConfiguration>();

            if (configuration is null)
            {
                output.WriteLine("Effective Configuration (EventStoreConnection) is null - see previous checks", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Effective Configuration is null"
                };
            }

            output.WriteLine($"Using EventStore @ '{configuration.Stream}' => {configuration.Stream}");

            using var eventStoreClient = ConnectToEventStore(configuration);

            try
            {
                return await CountEvents(eventStoreClient, configuration);
            }
            catch (Exception e)
            {
                output.WriteLine($"Error while Assigning event-handlers: {e.GetType().Name}; {e.Message}", 1);

                return new TestResult
                {
                    Result = false,
                    Message = "Error in EventHandler - see previous logs"
                };
            }
        }

        /// <inheritdoc />
        public string Name => "EventStore & Stream Availability";

        /// <summary>
        ///     subscribe to a stream and count the events in it
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private async Task<TestResult> CountEvents(EventStoreClient connection, EventStoreConnectionConfiguration configuration)
        {
            _output.WriteLine($"Counting all Events in Stream '{configuration.Stream}'", 1);

            var startTime = DateTime.Now;
            try
            {
                await foreach (var resolvedEvent in connection.ReadStreamAsync(Direction.Forwards,
                                                                               configuration.Stream,
                                                                               StreamPosition.Start,
                                                                               resolveLinkTos: true))
                {
                    _countedEvents += 1;
                }
            }
            catch (Exception e)
            {
                _output.WriteError($"connection dropped: {e}");
                _subscriptionDropped = true;
            }

            var stopTime = DateTime.Now;

            if (_subscriptionDropped)
            {
                _output.WriteLine($"Counted '{_countedEvents}' events in {FormatTime(stopTime - startTime)} before Subscription was dropped", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Subscription was dropped"
                };
            }

            _output.WriteLine($"Counted '{_countedEvents}' events in {FormatTime(stopTime - startTime)}", 1);

            return new TestResult
            {
                Result = true,
                Message = string.Empty
            };
        }

        /// <summary>
        ///     returns the given <see cref="TimeSpan"/> in a human-readable format
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        private string FormatTime(TimeSpan span)
        {
            if (span.TotalSeconds < 10)
                return span.TotalSeconds.ToString("#.#s");

            // don't care about sub-second precision
            var totalSeconds = (int) span.TotalSeconds;
            var totalMinutes = 0;

            while (totalSeconds > 60)
            {
                totalMinutes += 1;
                totalSeconds -= 60;
            }

            if (totalMinutes > 0)
                return $"{totalMinutes}m {totalSeconds}s";

            return $"{totalSeconds}s";
        }

        /// <summary>
        ///     open a connection to an EventStore as configured in <paramref name="configuration"/>
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private EventStoreClient ConnectToEventStore(EventStoreConnectionConfiguration configuration)
        {
            var settings = EventStoreClientSettings.Create(configuration.Uri);
            settings.ConnectionName = configuration.ConnectionName;
            settings.ConnectivitySettings.NodePreference = NodePreference.Random;
            settings.OperationOptions.TimeoutAfter = TimeSpan.FromMinutes(1);
            return new EventStoreClient(settings);
        }
    }
}