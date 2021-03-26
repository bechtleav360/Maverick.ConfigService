using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class EventDeserializer : IEventDeserializer
    {
        private readonly Dictionary<string, Type> _factoryAssociations = new Dictionary<string, Type>
        {
            {DomainEvent.GetEventType<ConfigurationBuilt>(), typeof(IDomainEventConverter<ConfigurationBuilt>)},
            {DomainEvent.GetEventType<DefaultEnvironmentCreated>(), typeof(IDomainEventConverter<DefaultEnvironmentCreated>)},
            {DomainEvent.GetEventType<EnvironmentCreated>(), typeof(IDomainEventConverter<EnvironmentCreated>)},
            {DomainEvent.GetEventType<EnvironmentDeleted>(), typeof(IDomainEventConverter<EnvironmentDeleted>)},
            {DomainEvent.GetEventType<EnvironmentLayersModified>(), typeof(IDomainEventConverter<EnvironmentLayersModified>)},
            {DomainEvent.GetEventType<EnvironmentLayerCreated>(), typeof(IDomainEventConverter<EnvironmentLayerCreated>)},
            {DomainEvent.GetEventType<EnvironmentLayerDeleted>(), typeof(IDomainEventConverter<EnvironmentLayerDeleted>)},
            {DomainEvent.GetEventType<EnvironmentLayerKeysModified>(), typeof(IDomainEventConverter<EnvironmentLayerKeysModified>)},
            {DomainEvent.GetEventType<EnvironmentLayerKeysImported>(), typeof(IDomainEventConverter<EnvironmentLayerKeysImported>)},
            {DomainEvent.GetEventType<StructureCreated>(), typeof(IDomainEventConverter<StructureCreated>)},
            {DomainEvent.GetEventType<StructureDeleted>(), typeof(IDomainEventConverter<StructureDeleted>)},
            {DomainEvent.GetEventType<StructureVariablesModified>(), typeof(IDomainEventConverter<StructureVariablesModified>)}
        };

        /// <inheritdoc cref="EventDeserializer" />
        public EventDeserializer(IServiceProvider provider, ILogger<EventDeserializer> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Provider = provider;
        }

        private ILogger Logger { get; }

        private IServiceProvider Provider { get; }

        /// <inheritdoc />
        public bool ToDomainEvent(StoredEvent storedEvent, out DomainEvent domainEvent)
        {
            if (_factoryAssociations.TryGetValue(storedEvent.EventType, out var factoryType))
                try
                {
                    var serializer = (IDomainEventConverter) Provider.GetService(factoryType);

                    domainEvent = serializer.DeserializeInstance(storedEvent.Data);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"could not deserialize data in '{storedEvent.EventType}' using '{factoryType.Name}'");
                }

            Logger.LogWarning($"event of type '{storedEvent.EventType}' ignored");
            domainEvent = null;
            return false;
        }

        /// <inheritdoc />
        public bool ToMetadata(StoredEvent storedEvent, out DomainEventMetadata metadata)
        {
            if (storedEvent.Metadata.IsEmpty)
            {
                Logger.LogTrace($"no metadata saved in event '{storedEvent.EventId}' " +
                                $"of type '{storedEvent.EventType}'");
                metadata = new DomainEventMetadata();
                return true;
            }

            if (_factoryAssociations.TryGetValue(storedEvent.EventType, out var factoryType))
                try
                {
                    var serializer = (IDomainEventConverter) Provider.GetService(factoryType);

                    metadata = serializer.DeserializeMetadata(storedEvent.Metadata);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"could not deserialize metadata in '{storedEvent.EventType}' using '{factoryType.Name}'");
                }

            Logger.LogWarning($"event of type '{storedEvent.EventType}' ignored");
            metadata = null;
            return false;
        }
    }
}