using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.Core.EventBus;
using Bechtle.A365.Core.EventBus.Events.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Polly;

namespace Bechtle.A365.ConfigService.Projection.Services
{
    /// <inheritdoc cref="IEventBusService" />
    public class EventBusService : IEventBusService, IDisposable
    {
        private static readonly Dictionary<Type, Action<string, EventMessage>> SubscribedEvents = new Dictionary<Type, Action<string, EventMessage>>();

        private readonly object _configChangingLock = new object();
        private readonly SemaphoreSlim _connectingSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Action<Exception> _onClosed;
        private readonly SemaphoreSlim _subscriptionChangingLock = new SemaphoreSlim(1, 1);
        private readonly IConfiguration _configuration;

        private readonly ISyncPolicy _retryPolicy = Policy.Handle<Exception>()
                                                          .WaitAndRetryForever(i =>
                                                          {
                                                              var maxTimeout = TimeSpan.FromMinutes(1);
                                                              var desiredTimeout = TimeSpan.FromSeconds(5).Multiply(i);

                                                              return desiredTimeout.TotalSeconds > maxTimeout.TotalSeconds
                                                                         ? maxTimeout
                                                                         : desiredTimeout;
                                                          });

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private WebSocketEventBusClient _client;
        private long _closing;
        private bool _disconnectedOnPurpose;
        private CancellationTokenSource _restartConnectionBecauseOfConfig = new CancellationTokenSource();
        private EventBusConnectionConfiguration _eventBusConnection;

        /// <inheritdoc />
        public EventBusService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            if (loggerFactory is null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _configuration = configuration;

            ChangeToken.OnChange(configuration.GetReloadToken, OnConfigurationChanged);

            _eventBusConnection = configuration.Get<ProjectionConfiguration>()
                                               .EventBusConnection;

            _loggerFactory = loggerFactory;

            _logger = _loggerFactory.CreateLogger<EventBusService>();

            _logger?.LogDebug($"Using endpoint '{_eventBusConnection.Server}' // '{_eventBusConnection.Hub}' to access {nameof(WebSocketEventBusClient)}.");

            _onClosed = async e => await ReconnectInternal(e);

            _retryPolicy.Execute(CreateWebsocketClient);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            if (_client == null)
                return;

            _client.HasClosed -= _onClosed;
            _client.Dispose();
        }

        /// <inheritdoc />
        public async Task Disconnect()
        {
            if (_client == null || 0 != Interlocked.Exchange(ref _closing, 1))
                return;

            _logger?.LogInformation($"Disconnecting {nameof(WebSocketEventBusClient)}.");

            // The disconnect will not happen, because connection is lost
            _disconnectedOnPurpose = true;

            // if some actions are still in queue
            _cancellationTokenSource.Cancel();

            await _client.Disconnect();
            _logger.LogInformation("Websocket service disconnected.");

            _cancellationTokenSource = new CancellationTokenSource();

            Interlocked.Exchange(ref _closing, 0);

            _logger?.LogInformation($"{nameof(WebSocketEventBusClient)} disconnected.");
        }

        /// <inheritdoc />
        public Task Publish(EventMessage message, string id)
        {
            Task.Run(() => PublishMessageInternalAsync(message, id), _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Publish(EventMessage message)
        {
            Task.Run(() => PublishMessageInternalAsync(message), _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task Reconnect(int maxRetries = 5)
        {
            _logger?.LogInformation($"Reconnecting {nameof(WebSocketEventBusClient)}.");

            await Disconnect();

            await ConnectClient();
        }

        /// <inheritdoc />
        public async Task Subscribe(Action<string, EventMessage> onEventOccured)
        {
            await ConnectClient();

            if (_client == null)
                return;

            _logger?.LogInformation($"Setting up generic subscription to '{_client.SignalRServer}'");

            await _client.Subscribe(onEventOccured);
        }

        /// <inheritdoc />
        public async Task Subscribe<TEvent>(Action<string, EventMessage> onEventOccured)
            where TEvent : IA365Event
        {
            if (SubscribedEvents.ContainsKey(typeof(TEvent)))
                return;

            var token = _cancellationTokenSource.Token;

            await ConnectClient();

            if (token.IsCancellationRequested)
                return;

            _logger?.LogInformation($"Setting up subscription - Event type: {typeof(TEvent).Name}");

            if (_client != null)
            {
                await _client.Subscribe<TEvent>(onEventOccured);
                _logger.LogInformation($"Subscribed event '{typeof(TEvent).Name}' (on '{_client.SignalRServer}').");
            }

            try
            {
                await _subscriptionChangingLock.WaitAsync(token);

                token.ThrowIfCancellationRequested();

                if (!SubscribedEvents.ContainsKey(typeof(TEvent)))
                    SubscribedEvents.Add(typeof(TEvent), onEventOccured);
            }
            finally
            {
                _subscriptionChangingLock.Release(1);
            }
        }

        /// <inheritdoc />
        public async Task Unsubscribe()
        {
            var token = _cancellationTokenSource.Token;

            await ConnectClient();

            if (token.IsCancellationRequested)
                return;

            if (_client == null)
                return;

            _logger?.LogInformation($"Cancelling generic subscription to '{_client.SignalRServer}'");

            await _client.Unsubscribe();
        }

        /// <inheritdoc />
        public async Task Unsubscribe<TEvent>()
            where TEvent : IA365Event
        {
            if (!SubscribedEvents.ContainsKey(typeof(TEvent)))
                return;

            var token = _cancellationTokenSource.Token;

            await ConnectClient();

            if (token.IsCancellationRequested)
                return;

            _logger?.LogInformation($"Cancelling subscription - Event type: {typeof(TEvent).Name}");

            if (_client != null)
            {
                await _client.Unsubscribe<TEvent>();
                _logger.LogInformation($"Unsubscribed event '{typeof(TEvent).Name}' (on '{_client.SignalRServer}').");
            }

            try
            {
                await _subscriptionChangingLock.WaitAsync(token);
                SubscribedEvents.Remove(typeof(TEvent));
            }
            finally
            {
                _subscriptionChangingLock.Release(1);
            }
        }

        private static async Task SubscribeInternalAsync(WebSocketEventBusClient client,
                                                         Type eventType,
                                                         Action<string, EventMessage> onEventOccurred)
        {
            if (client == null)
                return;

            var info = typeof(WebSocketEventBusClient)
                       .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                       .FirstOrDefault(m => m.Name == nameof(WebSocketEventBusClient.Subscribe) && m.IsGenericMethod)?
                       .MakeGenericMethod(eventType);

            if (info == null) throw new MissingMethodException(nameof(WebSocketEventBusClient), nameof(WebSocketEventBusClient.Subscribe));

            await (Task) info.Invoke(client, new object[] {onEventOccurred});
        }

        private static async Task UnsubscribeInternalAsync(WebSocketEventBusClient client, Type eventType)
        {
            if (client == null)
                return;

            var info = typeof(WebSocketEventBusClient)
                       .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                       .FirstOrDefault(m => m.Name == nameof(WebSocketEventBusClient.Unsubscribe) && m.IsGenericMethod)?
                       .MakeGenericMethod(eventType);

            if (info == null)
                throw new MissingMethodException(nameof(WebSocketEventBusClient), nameof(WebSocketEventBusClient.Unsubscribe));

            await (Task) info.Invoke(client, null);
        }

        private async Task PublishMessageInternalAsync(EventMessage message, string id)
        {
            await ConnectClient();

            if (_client == null)
                return;

            _logger?.LogDebug($"Publishing message to {nameof(id)} '{id}'. {nameof(EventMessage.Id)}: '{message?.Id}'; " +
                              $"{nameof(EventMessage.ClientId)}: '{message?.ClientId}'; " +
                              $"{nameof(EventMessage.EventType)}: '{message?.EventType}'");

            await _client.Publish(message, id);
        }

        private async Task ConnectClient(bool configurationChanged = false)
        {
            var cancellationToken = _cancellationTokenSource.Token;
            _disconnectedOnPurpose = false;

            // if the client is still not connected, just cancel the retry attempts and exit
            if (configurationChanged && 0 == Interlocked.Read(ref _closing)
                                     && _client != null && !_client.Connected)
            {
                _restartConnectionBecauseOfConfig.Cancel();
                return;
            }

            try
            {
                await _connectingSemaphore.WaitAsync(cancellationToken);

                if (!configurationChanged && (_client == null || _client.Connected) || 0 != Interlocked.Read(ref _closing))
                    return;

                // If the configuration has been changed, but the client is connected, disconnect and recreate the client
                // (this will also create a new client instance, if _client was not set before, because of missing endpoint configuration)
                if (configurationChanged && (_client == null || _client.Connected))
                    await ReCreateWebsocketClient();

                // if _client is still not assigned (because of missing endpoint data in configuration), ignore the connection attempt
                if (_client == null)
                    return;

                var policy = Policy
                             .Handle<Exception>()
                             .WaitAndRetryForeverAsync(
                                 count => count <= 10 ? TimeSpan.FromSeconds(Math.Pow(2, count) + 1) : TimeSpan.FromMinutes(20),
                                 (e, count, time) => _logger.LogWarning($"Could not establish connection to websocket service ('{_client.SignalRServer}'). " +
                                                                        $"Trying again in {time.TotalSeconds} sec. ({count}. attempt)"));
                do
                {
                    _restartConnectionBecauseOfConfig = new CancellationTokenSource();
                    var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                                                                                               _restartConnectionBecauseOfConfig.Token)
                                                                      .Token;

                    try
                    {
                        await policy.ExecuteAsync(async ctx =>
                        {
                            if (_client.Connected) return;

                            await _client.Connect();

                            if (!_client.Connected)
                                throw new Exception($"Could not establish connection to websocket service ('{_client.SignalRServer}').");
                        }, combinedCancellation);
                    }
                    catch
                    {
                        // if the config changes, and the related token throws an OperationCancellationException, a new web client should be created
                        // otherwise throw the exception
                        if (!_restartConnectionBecauseOfConfig.IsCancellationRequested)
                            throw;
                        await ReCreateWebsocketClient();
                    }
                } while (_restartConnectionBecauseOfConfig.IsCancellationRequested);

                _logger.LogInformation($"Connected to websocket service ({_client.SignalRServer}).");

                // restore subscriptions, because they might not be existent any more on websocket service (i.e. by crash of service).
                if (SubscribedEvents.Count > 0)
                {
                    foreach (var @event in SubscribedEvents)
                    {
                        _logger.LogDebug("Reconnect: Unsubscribing events.");
                        await UnsubscribeInternalAsync(_client, @event.Key);
                        _logger.LogDebug($"Reconnect: Subscribing event '{@event.Key}' again.");
                        await SubscribeInternalAsync(_client, @event.Key, @event.Value);
                    }

                    _logger.LogInformation("Subscriptions restored.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Operation cancelled while connecting.");
            }
            finally
            {
                _connectingSemaphore.Release();
            }
        }

        private void CreateWebsocketClient()
        {
            var endpointAddress = _eventBusConnection.Server;

            if (endpointAddress == null || !Uri.TryCreate(endpointAddress, UriKind.Absolute, out _))
            {
                _logger?.LogWarning($"Configuration issue: Cannot find valid endpoint for EventBus " +
                                    $"in service settings (current value: '{endpointAddress}'). Cannot connect websocket EventBus.");
                return;
            }

            _client = new WebSocketEventBusClient(endpointAddress, _loggerFactory);

            _logger?.LogDebug($"Using endpoint '{endpointAddress}' to access {nameof(WebSocketEventBusClient)}.");

            _client.HasClosed += _onClosed;
        }

        private void LogExceptionsRecursively(Exception exception)
        {
            if (exception == null)
                return;

            _logger.LogError(exception, $"Exception of type '{exception.GetType()}' occurred. '{exception.Message}'");

            if (exception.InnerException != null)
                LogExceptionsRecursively(exception.InnerException);
        }

        private void OnConfigurationChanged()
        {
            lock (_configChangingLock)
            {
                var config = _configuration.Get<ProjectionConfiguration>().EventBusConnection;

                // only change the own configuration, if the newer one is different than the existing one
                if (!_eventBusConnection.Server.Equals(config.Server) && !_eventBusConnection.Hub.Equals(config.Hub))
                    return;

                _eventBusConnection = config;
                _logger.LogInformation("Configuration of EventBus endpoint changed.");
                ConnectClient(true).RunSync();
            }
        }

        private async Task PublishMessageInternalAsync(EventMessage message)
        {
            await ConnectClient();

            if (_client == null)
                return;

            _logger?.LogDebug($"Publishing message. {nameof(EventMessage.Id)}: '{message?.Id}'; " +
                              $"{nameof(EventMessage.ClientId)}: '{message?.ClientId}'; " +
                              $"{nameof(EventMessage.EventType)}: '{message?.EventType}'");

            await _client.Publish(message);
        }

        private async Task ReconnectInternal(Exception exception)
        {
            // don't reconnect, if the disconnect was triggered on purpose
            if (_disconnectedOnPurpose) return;

            if (exception != null)
                LogExceptionsRecursively(exception);

            _logger.LogInformation("Connection to websocket service closed. Trying to reconnect.");

            await ConnectClient();
        }

        private async Task ReCreateWebsocketClient()
        {
            if (_client != null)
            {
                _client.HasClosed -= _onClosed;
                try
                {
                    await _client.Disconnect();
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, $"Could not disconnect existing client. {e.Message}");
                }
                finally
                {
                    _client.Dispose();
                }
            }

            _retryPolicy.Execute(CreateWebsocketClient);
        }
    }

    public interface IEventBusService
    {
        /// <summary>
        ///     disconnect from the EventBus
        /// </summary>
        /// <returns></returns>
        Task Disconnect();

        /// <summary>
        ///     publish a message to a specific client
        /// </summary>
        /// <param name="message"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task Publish(EventMessage message, string id);

        /// <summary>
        ///     publish a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Publish(EventMessage message);

        /// <summary>
        ///     try to reconnect to the bus
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <returns></returns>
        Task Reconnect(int maxRetries = 5);

        /// <summary>
        ///     subscribe to the general-event-group
        /// </summary>
        /// <param name="onEventOccured"></param>
        /// <returns></returns>
        Task Subscribe(Action<string, EventMessage> onEventOccured);

        /// <summary>
        ///     subscribe to a specific group of events
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="onEventOccured"></param>
        /// <returns></returns>
        Task Subscribe<TEvent>(Action<string, EventMessage> onEventOccured) where TEvent : IA365Event;

        /// <summary>
        ///     unsubscribe from the general-event-group
        /// </summary>
        /// <returns></returns>
        Task Unsubscribe();

        /// <summary>
        ///     unsubscribe from a group of events
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        Task Unsubscribe<TEvent>() where TEvent : IA365Event;
    }
}