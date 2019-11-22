using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common.Converters;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    public class EventDeserializer : IEventDeserializer
    {
        private readonly Dictionary<string, Type> _factoryAssociations = new Dictionary<string, Type>
        {
            {DomainEvent.GetEventType<ConfigurationBuilt>(), typeof(IDomainEventConverter<ConfigurationBuilt>)},
            {DomainEvent.GetEventType<DefaultEnvironmentCreated>(), typeof(IDomainEventConverter<DefaultEnvironmentCreated>)},
            {DomainEvent.GetEventType<EnvironmentCreated>(), typeof(IDomainEventConverter<EnvironmentCreated>)},
            {DomainEvent.GetEventType<EnvironmentDeleted>(), typeof(IDomainEventConverter<EnvironmentDeleted>)},
            {DomainEvent.GetEventType<EnvironmentKeysModified>(), typeof(IDomainEventConverter<EnvironmentKeysModified>)},
            {DomainEvent.GetEventType<EnvironmentKeysImported>(), typeof(IDomainEventConverter<EnvironmentKeysImported>)},
            {DomainEvent.GetEventType<StructureCreated>(), typeof(IDomainEventConverter<StructureCreated>)},
            {DomainEvent.GetEventType<StructureDeleted>(), typeof(IDomainEventConverter<StructureDeleted>)},
            {DomainEvent.GetEventType<StructureVariablesModified>(), typeof(IDomainEventConverter<StructureVariablesModified>)}
        };

        /// <inheritdoc />
        public EventDeserializer(IServiceProvider provider, ILogger<EventDeserializer> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Provider = provider;
        }

        private ILogger Logger { get; }

        private IServiceProvider Provider { get; }

        /// <inheritdoc />
        public bool ToDomainEvent(ResolvedEvent resolvedEvent, out DomainEvent domainEvent)
        {
            if (_factoryAssociations.TryGetValue(resolvedEvent.OriginalEvent.EventType, out var factoryType))
                try
                {
                    var serializer = (IDomainEventConverter) Provider.GetService(factoryType);

                    domainEvent = serializer.DeserializeInstance(resolvedEvent.OriginalEvent.Data);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"could not deserialize data in '{resolvedEvent.OriginalEvent.EventType}' using '{factoryType.Name}'");
                }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            domainEvent = null;
            return false;
        }

        public bool ToMetadata(ResolvedEvent resolvedEvent, out DomainEventMetadata metadata)
        {
            if (resolvedEvent.OriginalEvent.Metadata?.Any() != true)
            {
                Logger.LogTrace($"no metadata saved in event '{resolvedEvent.OriginalEvent.EventId}' " +
                                $"of type '{resolvedEvent.OriginalEvent.EventType}'");
                metadata = new DomainEventMetadata();
                return true;
            }

            if (_factoryAssociations.TryGetValue(resolvedEvent.OriginalEvent.EventType, out var factoryType))
                try
                {
                    var serializer = (IDomainEventConverter) Provider.GetService(factoryType);

                    metadata = serializer.DeserializeMetadata(resolvedEvent.OriginalEvent.Metadata);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"could not deserialize metadata in '{resolvedEvent.OriginalEvent.EventType}' using '{factoryType.Name}'");
                }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            metadata = null;
            return false;
        }
    }
}