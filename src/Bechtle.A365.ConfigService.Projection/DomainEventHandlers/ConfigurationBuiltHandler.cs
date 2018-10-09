using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;

// using Bechtle.A365.Core.EventBus.Abstraction;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationDatabase _database;
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;

        private readonly IJsonTranslator _translator;
        // private readonly IEventBus _eventBus;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database,
                                         IConfigurationCompiler compiler,
                                         IConfigurationParser parser,
                                         IJsonTranslator translator)
            // IEventBus eventBus)
        {
            _database = database;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            //_eventBus = eventBus;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
            var structureResult = await _database.GetStructure(domainEvent.Identifier.Structure);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _database.GetEnvironmentWithInheritance(domainEvent.Identifier.Environment);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var defaultEnvironmentResult = await _database.GetDefaultEnvironment(domainEvent.Identifier.Environment.Category);
            if (defaultEnvironmentResult.IsError)
                throw new Exception(defaultEnvironmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;
            var defaultEnvironmentSnapshot = defaultEnvironmentResult.Data;

            var compiledRepository = await _compiler.Compile(defaultEnvironmentSnapshot.Data,
                                                             environmentSnapshot.Data,
                                                             _parser,
                                                             CompilationOptions.EnvFromEnv);

            var compiled = await _compiler.Compile(compiledRepository,
                                                   structureSnapshot.Data,
                                                   _parser,
                                                   CompilationOptions.StructFromEnv);

            var json = _translator.ToJson(compiled)
                                  .ToString();

            await _database.SaveConfiguration(environmentSnapshot,
                                              structureSnapshot,
                                              compiled,
                                              json,
                                              domainEvent.ValidFrom,
                                              domainEvent.ValidTo);

            // await _eventBus.Publish(new EventBusMessages.OnVersionBuilt());
        }
    }
}