﻿using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using Bechtle.A365.ConfigService.Utilities;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// god-damn-it fuck you 'EventStore' for creating 'ILogger' when that is basically a core component of the eco-system
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    /// </summary>
    public class ConfigStore : IConfigStore
    {
        private readonly IEventStoreConnection _eventStore;

        private readonly ILogger _logger;

        private readonly string _stream;

        /// <summary>
        /// </summary>
        /// <param name="provider"></param>
        public ConfigStore(IServiceProvider provider)
        {
            _stream = "ConfigStream";

            _logger = provider.GetService<ILoggerFactory>()
                              .CreateLogger<ConfigStore>();

            var uri = new Uri("tcp://admin:changeit@localhost:1113");
            var connectionName = "ConfigService";

            _logger.LogInformation($"connecting to '{uri}' using connectionName: '{connectionName}'");

            _eventStore = EventStoreConnection.Create(uri, connectionName);

            _eventStore.ConnectAsync().RunSync();
        }

        /// <summary>
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        public async Task WriteEvent(DomainEvent domainEvent)
        {
            _logger.LogDebug($"{nameof(WriteEvent)}('{domainEvent.GetType().Name}')");

            var (data, metadata) = DomainEventFactory.Serialize(domainEvent);

            var eventData = new EventData(Guid.NewGuid(),
                                          domainEvent.EventType,
                                          false,
                                          data,
                                          metadata);

            _logger.LogInformation("sending " +
                                   $"EventId: '{eventData.EventId}'; " +
                                   $"Type: '{eventData.Type}'; " +
                                   $"IsJson: '{eventData.IsJson}'; " +
                                   $"Data: {eventData.Data.Length} bytes; " +
                                   $"Metadata: {eventData.Metadata.Length} bytes;");

            var result = await _eventStore.AppendToStreamAsync(_stream, ExpectedVersion.Any, eventData);

            _logger.LogDebug($"sent event '{eventData.EventId}': " +
                             $"NextExpectedVersion: {result.NextExpectedVersion}; " +
                             $"CommitPosition: {result.LogPosition.CommitPosition}; " +
                             $"PreparePosition: {result.LogPosition.PreparePosition};");
        }
    }
}