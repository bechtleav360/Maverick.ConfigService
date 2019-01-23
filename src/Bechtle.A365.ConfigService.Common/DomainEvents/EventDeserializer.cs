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
        /// <inheritdoc />
        public EventDeserializer(IServiceProvider provider, ILogger<EventDeserializer> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(Logger));
            Provider = provider;
        }

        private ILogger Logger { get; }

        private IServiceProvider Provider { get; }

        /// <inheritdoc />
        public DomainEvent ToDomainEvent(ResolvedEvent resolvedEvent)
        {
            var factoryAssociations = new Dictionary<string, Type>
            {
                {DomainEvent.GetEventType<ConfigurationBuilt>(), typeof(IDomainEventConverter<ConfigurationBuilt>)},
                {DomainEvent.GetEventType<DefaultEnvironmentCreated>(), typeof(IDomainEventConverter<DefaultEnvironmentCreated>)},
                {DomainEvent.GetEventType<EnvironmentCreated>(), typeof(IDomainEventConverter<EnvironmentCreated>)},
                {DomainEvent.GetEventType<EnvironmentDeleted>(), typeof(IDomainEventConverter<EnvironmentDeleted>)},
                {DomainEvent.GetEventType<EnvironmentKeysModified>(), typeof(IDomainEventConverter<EnvironmentKeysModified>)},
                {DomainEvent.GetEventType<StructureCreated>(), typeof(IDomainEventConverter<StructureCreated>)},
                {DomainEvent.GetEventType<StructureDeleted>(), typeof(IDomainEventConverter<StructureDeleted>)},
                {DomainEvent.GetEventType<StructureVariablesModified>(), typeof(IDomainEventConverter<StructureVariablesModified>)}
            };

            foreach (var factory in factoryAssociations)
            {
                if (factory.Key != resolvedEvent.OriginalEvent.EventType)
                    continue;

                var serializer = (IDomainEventConverter) Provider.GetService(factory.Value);

                return serializer.Deserialize(resolvedEvent.OriginalEvent.Data, resolvedEvent.OriginalEvent.Metadata);
            }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            return null;
        }
    }
}