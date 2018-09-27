using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Dto.EventFactories;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bechtle.A365.ConfigService.Projection
{
    public class EventResolver : IEventResolver
    {
        private ILogger Logger { get; }
        private IServiceProvider Provider { get; }

        /// <inheritdoc />
        public EventResolver(IServiceProvider provider, ILogger<EventResolver> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(Logger));
            Provider = provider;
        }

        /// <inheritdoc />
        public DomainEvent ToDomainEvent(ResolvedEvent resolvedEvent)
        {
            var factoryAssociations = new Dictionary<string, Type>
            {
                {DomainEvent.GetEventType<ConfigurationBuilt>(), typeof(IDomainEventSerializer<ConfigurationBuilt>)},
                {DomainEvent.GetEventType<EnvironmentCreated>(), typeof(IDomainEventSerializer<EnvironmentCreated>)},
                {DomainEvent.GetEventType<EnvironmentDeleted>(), typeof(IDomainEventSerializer<EnvironmentDeleted>)},
                {DomainEvent.GetEventType<EnvironmentKeysModified>(), typeof(IDomainEventSerializer<EnvironmentKeysModified>)},
                {DomainEvent.GetEventType<StructureCreated>(), typeof(IDomainEventSerializer<StructureCreated>)},
                {DomainEvent.GetEventType<StructureDeleted>(), typeof(IDomainEventSerializer<StructureDeleted>)}
            };

            foreach (var factory in factoryAssociations)
            {
                if (factory.Key != resolvedEvent.OriginalEvent.EventType)
                    continue;

                var serializer = (IDomainEventSerializer) Provider.GetService(factory.Value);

                return serializer.Deserialize(resolvedEvent.OriginalEvent.Data, resolvedEvent.OriginalEvent.Metadata);
            }

            Logger.LogWarning($"event of type '{resolvedEvent.OriginalEvent.EventType}' ignored");
            return null;
        }
    }
}