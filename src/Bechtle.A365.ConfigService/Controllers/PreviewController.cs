using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    [Route(ApiBaseRoute + "preview")]
    public class PreviewController : ControllerBase
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IProjectionStore _store;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public PreviewController(IServiceProvider provider, 
                                 ILogger<PreviewController> logger,
                                 IConfigurationCompiler compiler,
                                 IProjectionStore store,
                                 IConfigurationParser parser,
                                 IJsonTranslator translator)
            : base(provider, logger)
        {
            _compiler = compiler;
            _store = store;
            _parser = parser;
            _translator = translator;
        }

        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        public async Task<IActionResult> PreviewConfiguration([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var defaultEnvId = new EnvironmentIdentifier(environmentCategory, "Default");
            var structId = new StructureIdentifier(structureName, structureVersion);

            var structureResult = await _store.Structures.GetKeys(structId);
            if (structureResult.IsError)
                throw new Exception(structureResult.Message);

            var environmentResult = await _store.Environments.GetKeys(envId);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var defaultEnvironmentResult = await _store.Environments.GetKeys(defaultEnvId);
            if (defaultEnvironmentResult.IsError)
                throw new Exception(defaultEnvironmentResult.Message);

            var structureSnapshot = structureResult.Data;
            var environmentSnapshot = environmentResult.Data;
            var defaultEnvironmentSnapshot = defaultEnvironmentResult.Data;

            var compiledRepository = await _compiler.Compile(defaultEnvironmentSnapshot,
                                                             environmentSnapshot,
                                                             _parser,
                                                             CompilationOptions.EnvFromEnv);

            var compiled = await _compiler.Compile(compiledRepository,
                                                   structureSnapshot,
                                                   _parser,
                                                   CompilationOptions.StructFromEnv);

            var json = _translator.ToJson(compiled);

            return Ok(json);
        }
    }
}