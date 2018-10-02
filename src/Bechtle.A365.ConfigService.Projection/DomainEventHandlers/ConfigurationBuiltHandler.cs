using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.Compilation;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Newtonsoft.Json;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class ConfigurationBuiltHandler : IDomainEventHandler<ConfigurationBuilt>
    {
        private readonly IConfigurationDatabase _database;
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IDictionaryToJsonConverter _converter;

        /// <inheritdoc />
        public ConfigurationBuiltHandler(IConfigurationDatabase database,
                                         IConfigurationCompiler compiler,
                                         IConfigurationParser parser,
                                         IDictionaryToJsonConverter converter)
        {
            _database = database;
            _compiler = compiler;
            _parser = parser;
            _converter = converter;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(ConfigurationBuilt domainEvent)
        {
            var structureResult = await _database.GetStructure(domainEvent.Structure);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _database.GetEnvironment(domainEvent.Environment);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;

            var compiled = await _compiler.Compile(environmentSnapshot.Data,
                                                   structureSnapshot.Data,
                                                   _parser);

            var json = _converter.ConvertToJson(compiled)
                                 .ToString(Formatting.None);

            await _database.SaveConfiguration(environmentSnapshot,
                                              structureSnapshot,
                                              compiled,
                                              json);
        }
    }
}