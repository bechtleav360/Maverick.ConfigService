using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Configuration;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class EventStoreConnectionCheck : IConnectionCheck
    {
        /// <summary>
        ///     number of events counted in the target stream
        /// </summary>
        private int _countedEvents;

        /// <summary>
        ///     flag indicating if the live-processing of events has started - meaning all events up to now are counted
        /// </summary>
        private bool _liveProcessingEvents;

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

            using var connection = MakeConnection(configuration);
            RegisterEvents(connection);

            try
            {
                await connection.ConnectAsync();

                return await CountEvents(connection, configuration);
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
            finally
            {
                DeRegisterEvents(connection);
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
        private async Task<TestResult> CountEvents(IEventStoreConnection connection, EventStoreConnectionConfiguration configuration)
        {
            _output.WriteLine($"Counting all Events in Stream '{configuration.Stream}'", 1);

            var startTime = DateTime.Now;
            var stopTime = DateTime.Now;

            // subscribe to the stream to count the number of events contained in it
            connection.SubscribeToStreamFrom(configuration.Stream,
                                             StreamCheckpoint.StreamStart,
                                             new CatchUpSubscriptionSettings(128, 256, false, true, "ConfigService.CLI.ConnectionTest"),
                                             (subscription, @event) => { _countedEvents += 1; },
                                             subscription =>
                                             {
                                                 _output.WriteLine($"Subscription to '{subscription.SubscriptionName}' opened", 1);
                                                 stopTime = DateTime.Now;
                                                 _liveProcessingEvents = true;
                                             },
                                             (subscription, reason, exception) =>
                                             {
                                                 _output.WriteLine(
                                                     $"Subscription to '{subscription.SubscriptionName}' dropped: " +
                                                     $"{reason}; {exception.GetType().Name} {exception.Message}", 1);
                                                 stopTime = DateTime.Now;
                                                 _subscriptionDropped = true;
                                             });

            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            } while (!_liveProcessingEvents && !_subscriptionDropped);

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
        ///     remove the registered events from the given <paramref name="connection"/>
        /// </summary>
        /// <param name="connection"></param>
        private void DeRegisterEvents(IEventStoreConnection connection)
        {
            connection.AuthenticationFailed -= OnAuthenticationFailed;
            connection.Closed -= OnClosed;
            connection.Connected -= OnConnected;
            connection.Disconnected -= OnDisconnected;
            connection.ErrorOccurred -= OnErrorOccurred;
            connection.Reconnecting -= OnReconnecting;
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
        private IEventStoreConnection MakeConnection(EventStoreConnectionConfiguration configuration)
            => EventStoreConnection.Create(ConnectionSettings.Create()
                                                             .KeepReconnecting()
                                                             .KeepRetrying(),
                                           new Uri(configuration.Uri),
                                           configuration.ConnectionName);

        private void OnAuthenticationFailed(object sender, ClientAuthenticationFailedEventArgs args)
            => _output.WriteLine($"Authentication to EventStore failed: {args.Reason}", 1);

        private void OnClosed(object sender, ClientClosedEventArgs args)
            => _output.WriteLine($"Connection to EventStore was Closed: {args.Reason}", 1);

        private void OnConnected(object sender, ClientConnectionEventArgs args)
            => _output.WriteLine("Connection to EventStore was Opened", 1);

        private void OnDisconnected(object sender, ClientConnectionEventArgs args)
            => _output.WriteLine("Connection to EventStore was Disconnected", 1);

        private void OnErrorOccurred(object sender, ClientErrorEventArgs args)
            => _output.WriteLine($"Error Occured in EventStore Connection: {args.Exception.GetType().Name}; {args.Exception.Message}", 1);

        private void OnReconnecting(object sender, ClientReconnectingEventArgs args)
            => _output.WriteLine("Connection to EventStore is being Re-Established", 1);

        /// <summary>
        ///     register handlers for all possible events in the given <paramref name="connection"/>
        /// </summary>
        /// <param name="connection"></param>
        private void RegisterEvents(IEventStoreConnection connection)
        {
            connection.AuthenticationFailed += OnAuthenticationFailed;
            connection.Closed += OnClosed;
            connection.Connected += OnConnected;
            connection.Disconnected += OnDisconnected;
            connection.ErrorOccurred += OnErrorOccurred;
            connection.Reconnecting += OnReconnecting;
        }
    }
}