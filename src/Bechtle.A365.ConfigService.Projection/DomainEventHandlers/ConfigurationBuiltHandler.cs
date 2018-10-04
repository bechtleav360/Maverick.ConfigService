using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.Compilation;
using Bechtle.A365.ConfigService.Projection.DataStorage;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationDatabase _database;
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database,
                                         IConfigurationCompiler compiler,
                                         IConfigurationParser parser,
                                         IJsonTranslator translator)
        {
            _database = database;
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
            var structureResult = await _database.GetStructure(domainEvent.Identifier.Structure);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _database.GetEnvironment(domainEvent.Identifier.Environment);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;

            var compiled = await _compiler.Compile(environmentSnapshot.Data,
                                                   structureSnapshot.Data,
                                                   _parser);

            var json = _translator.ToJson(compiled)
                                 .ToString();

            await _database.SaveConfiguration(environmentSnapshot,
                                              structureSnapshot,
                                              compiled,
                                              json);
        }
    }
}