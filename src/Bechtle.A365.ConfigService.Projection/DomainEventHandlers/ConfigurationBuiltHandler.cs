using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Events;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationDatabase _database;
        private readonly IEventBus _eventBus;
        private readonly ILogger<ConfigurationBuiltHandler> _logger;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database,
                                         IConfigurationCompiler compiler,
                                         IConfigurationParser parser,
                                         IJsonTranslator translator,
                                         IEventBus eventBus,
                                         ILogger<ConfigurationBuiltHandler> logger)
        {
            _database = database;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _eventBus = eventBus;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
            var envId = domainEvent.Identifier.Environment;
            var structId = domainEvent.Identifier.Structure;

            _logger.LogInformation($"building Configuration for {envId} {structId}");

            var structureResult = await _database.GetStructure(structId);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _database.GetEnvironmentWithInheritance(envId);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;

            var environmentInfo = new EnvironmentCompilationInfo
            {
                Name = $"{envId.Category}/{envId.Name}",
                Keys = environmentSnapshot.Data
            };

            var structureInfo = new StructureCompilationInfo
            {
                Name = $"{structId.Name}/{structId.Version}",
                Keys = structureSnapshot.Data,
                Variables = structureSnapshot.Variables
            };

            var compiled = _compiler.Compile(environmentInfo,
                                             structureInfo,
                                             _parser);

            var json = _translator.ToJson(compiled.CompiledConfiguration)
                                  .ToString();

            await _database.SaveConfiguration(environmentSnapshot,
                                              structureSnapshot,
                                              compiled.CompiledConfiguration,
                                              json,
                                              compiled.GetUsedKeys(),
                                              domainEvent.ValidFrom,
                                              domainEvent.ValidTo);

            // await PublishConfigurationChangedEvent(domainEvent);
        }

        private async Task PublishConfigurationChangedEvent(ConfigurationBuilt domainEvent)
        {
            /*
             * is the new config active right now?
             * if so, then publish a ConfigurationPublished event
             *
             * open ended indefinite
             * null => null = publish
             *
             * from before, indefinite
             * earlier => null = publish
             *
             * in the past
             * earlier => earlier = no publish
             *
             * from before to the future
             * earlier => later = publish
             *
             * in the future, indefinite
             * later => null = no publish
             *
             * in the future, to the past => impossible
             * later => earlier = no publish
             *
             * in the future, to later in the future
             * later => later = no publish
             */
            var from = domainEvent.ValidFrom;
            var to = domainEvent.ValidTo;
            var now = DateTime.UtcNow;

            if (from is null && to is null ||
                !(from is null) && from < now && to is null ||
                !(from is null) && from < now && !(to is null) && to > now)
                await _eventBus.Publish(new EventMessage
                {
                    Event = new OnConfigurationPublished
                    {
                        EnvironmentCategory = domainEvent.Identifier.Environment.Category,
                        EnvironmentName = domainEvent.Identifier.Environment.Name,
                        StructureName = domainEvent.Identifier.Structure.Name,
                        StructureVersion = domainEvent.Identifier.Structure.Version
                    }
                });
        }
    }
}