using System;
using System.Collections.Generic;
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
            Logger = logger ?? throw new ArgumentNullException(nameof(Logger));
            Provider = provider;
        }

        private ILogger Logger { get; }

        private IServiceProvider Provider { get; }

        /// <inheritdoc />
        public bool ToDomainEvent(ResolvedEvent resolvedEvent, out DomainEvent domainEvent)
        {
            if (_factoryAssociations.TryGetValue(resolvedEvent.OriginalEvent.EventType, out var factoryType))
            {
                try
                {
                    var serializer = (IDomainEventConverter) Provider.GetService(factoryType);

                    domainEvent = serializer.Deserialize(resolvedEvent.OriginalEvent.Data, resolvedEvent.OriginalEvent.Metadata);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"could not deserialize data in '{resolvedEvent.OriginalEvent.EventType}' using '{factoryType.Name}': {e}");
                }
            }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            domainEvent = null;
            return false;
        }
    }
}