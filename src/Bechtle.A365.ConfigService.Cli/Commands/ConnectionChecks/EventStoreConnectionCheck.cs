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

        private FormattedOutput _output;

        /// <summary>
        ///     Time at which the event-count was started
        /// </summary>
        private DateTime _startTime = DateTime.MinValue;

        /// <summary>
        ///     Time at which the event-count was finished or dropped
        /// </summary>
        private DateTime _stopTime = DateTime.MaxValue;

        /// <summary>
        ///     flag indicating if the Subscription has been dropped before finishing the event-count - see <see cref="_countedEvents" />
        /// </summary>
        private bool _subscriptionDropped;

        /// <inheritdoc />
        public async Task<TestResult> Execute(FormattedOutput output, TestParameters parameters)
        {
            _output = output;

            output.Line("Connecting to EventStore using Effective Configuration");

            var configuration = ConfigurationCheck.EffectiveConfiguration.Get<ConfigServiceConfiguration>();

            if (configuration?.EventStoreConnection is null)
            {
                output.Line("Effective Configuration (EventStoreConnection) is null - see previous checks", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Effective Configuration is null"
                };
            }

            output.Line($"Using EventStore @ '{configuration.EventStoreConnection.Stream}' => {configuration.EventStoreConnection.Stream}");

            using (var connection = MakeConnection(configuration))
            {
                RegisterEvents(connection);

                try
                {
                    await connection.ConnectAsync();

                    return await CountEvents(connection, configuration);
                }
                catch (Exception e)
                {
                    output.Line($"Error while Assigning event-handlers: {e.GetType().Name}; {e.Message}", 1);

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
        }

        /// <inheritdoc />
        public string Name => "EventStore & Stream Availability";

        /// <summary>
        ///     subscribe to a stream and count the events in it
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private async Task<TestResult> CountEvents(IEventStoreConnection connection, ConfigServiceConfiguration configuration)
        {
            _output.Line($"Counting all Events in Stream '{configuration.EventStoreConnection.Stream}'", 1);

            _startTime = DateTime.Now;

            // subscribe to the stream to count the number of events contained in it
            connection.SubscribeToStreamFrom(configuration.EventStoreConnection.Stream,
                                             StreamCheckpoint.StreamStart,
                                             new CatchUpSubscriptionSettings(128, 256, false, true, "ConfigService.CLI.ConnectionTest"),
                                             (subscription, @event) => { _countedEvents += 1; },
                                             subscription =>
                                             {
                                                 _output.Line($"Subscription to '{subscription.SubscriptionName}' opened", 1);
                                                 _stopTime = DateTime.Now;
                                                 _liveProcessingEvents = true;
                                             },
                                             (subscription, reason, exception) =>
                                             {
                                                 _output.Line(
                                                     $"Subscription to '{subscription.SubscriptionName}' dropped: " +
                                                     $"{reason}; {exception.GetType().Name} {exception.Message}", 1);
                                                 _stopTime = DateTime.Now;
                                                 _subscriptionDropped = true;
                                             });

            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            } while (!_liveProcessingEvents && !_subscriptionDropped);

            if (_subscriptionDropped)
            {
                _output.Line($"Counted '{_countedEvents}' events in {FormatTime(_stopTime - _startTime)} before Subscription was dropped", 1);
                return new TestResult
                {
                    Result = false,
                    Message = "Subscription was dropped"
                };
            }

            _output.Line($"Counted '{_countedEvents}' events in {FormatTime(_stopTime - _startTime)}", 1);

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
        private IEventStoreConnection MakeConnection(ConfigServiceConfiguration configuration)
            => EventStoreConnection.Create(ConnectionSettings.Create()
                                                             .KeepReconnecting()
                                                             .KeepRetrying(),
                                           new Uri(configuration.EventStoreConnection.Uri),
                                           configuration.EventStoreConnection.ConnectionName);

        private void OnAuthenticationFailed(object sender, ClientAuthenticationFailedEventArgs args)
            => _output.Line($"Authentication to EventStore failed: {args.Reason}", 1);

        private void OnClosed(object sender, ClientClosedEventArgs args)
            => _output.Line($"Connection to EventStore was Closed: {args.Reason}", 1);

        private void OnConnected(object sender, ClientConnectionEventArgs args)
            => _output.Line("Connection to EventStore was Opened", 1);

        private void OnDisconnected(object sender, ClientConnectionEventArgs args)
            => _output.Line("Connection to EventStore was Disconnected", 1);

        private void OnErrorOccurred(object sender, ClientErrorEventArgs args)
            => _output.Line($"Error Occured in EventStore Connection: {args.Exception.GetType().Name}; {args.Exception.Message}", 1);

        private void OnReconnecting(object sender, ClientReconnectingEventArgs args)
            => _output.Line("Connection to EventStore is being Re-Established", 1);

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