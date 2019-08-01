using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Models.V0;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.NonVersioned
{
    /// <summary>
    ///     preview the Results of Building different Configurations
    /// </summary>
    [Route("preview")]
    public class PreviewController : ControllerBase
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IProjectionStore _store;
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

        /// <summary>
        ///     preview the Result of building an existing Environment and a Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = "Deprecated_Fallback_PreviewConfigurationWithStoredValues")]
        public async Task<IActionResult> PreviewConfiguration([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structId = new StructureIdentifier(structureName, structureVersion);

            var structureKeyResult = await _store.Structures.GetKeys(structId, QueryRange.All);
            if (structureKeyResult.IsError)
                throw new Exception(structureKeyResult.Message);

            var structureVariableResult = await _store.Structures.GetVariables(structId, QueryRange.All);
            if (structureVariableResult.IsError)
                throw new Exception(structureVariableResult.Message);

            var environmentResult = await _store.Environments.GetKeys(envId, QueryRange.All);
            if (environmentResult.IsError)
                throw new Exception(environmentResult.Message);

            var structureSnapshot = structureKeyResult.Data;
            var variableSnapshot = structureVariableResult.Data;
            var environmentSnapshot = environmentResult.Data;

            var environmentInfo = new EnvironmentCompilationInfo
            {
                Name = $"{envId.Category}/{envId.Name}",
                Keys = environmentSnapshot
            };
            var structureInfo = new StructureCompilationInfo
            {
                Name = $"{structId.Name}/{structId.Version}",
                Keys = structureSnapshot,
                Variables = variableSnapshot
            };

            var compiled = _compiler.Compile(environmentInfo,
                                             structureInfo,
                                             _parser);

            var json = _translator.ToJson(compiled.CompiledConfiguration);

            return Ok(json);
        }

        /// <summary>
        ///     preview the Result of building the given Environment and Structure
        /// </summary>
        /// <param name="previewOptions"></param>
        /// <returns></returns>
        [ApiVersion(ApiVersions.V0, Deprecated = ApiDeprecation.V0)]
        [HttpPost(Name = "Deprecated_Fallback_PreviewConfigurationWithGivenValues")]
        public IActionResult PreviewConfiguration([FromBody] PreviewContainer previewOptions)
        {
            if (previewOptions is null)
                return BadRequest("no preview-data received");

            if (previewOptions.Structure is null)
                return BadRequest("no structure-data received");

            if (previewOptions.Variables is null)
                return BadRequest("no variable-data received");

            if (previewOptions.Environment is null)
                return BadRequest("no environment-data received");

            var environmentInfo = new EnvironmentCompilationInfo
            {
                Name = "Intermediate-Preview-Environment",
                Keys = previewOptions.Environment
            };
            var structureInfo = new StructureCompilationInfo
            {
                Name = "Intermediate-Preview-Structure",
                Keys = previewOptions.Structure,
                Variables = previewOptions.Variables
            };

            var compiled = _compiler.Compile(environmentInfo,
                                             structureInfo,
                                             _parser);

            var json = _translator.ToJson(compiled.CompiledConfiguration);

            return Ok(new PreviewResult
            {
                Map = compiled.CompiledConfiguration.ToImmutableSortedDictionary(),
                Json = json,
                UsedKeys = compiled.GetUsedKeys().Where(key => environmentInfo.Keys.ContainsKey(key))
            });
        }
    }
}