using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     preview the Results of Building different Configurations
    /// </summary>
    [ApiVersion(ApiVersion)]
    [Route(ApiBaseRoute + "preview")]
    public class PreviewController : ControllerBase
    {
        private readonly V0.PreviewController _previousVersion;
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
                                 IJsonTranslator translator,
                                 V0.PreviewController previousVersion)
            : base(provider, logger)
        {
            _compiler = compiler;
            _store = store;
            _parser = parser;
            _translator = translator;
            _previousVersion = previousVersion;
        }

        /// <summary>
        ///     preview the Result of building an existing Environment and a Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = ApiVersionFormatted + "PreviewConfigurationWithStoredValues")]
        public Task<IActionResult> PreviewConfiguration([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion)
            => _previousVersion.PreviewConfiguration(environmentCategory, environmentName, structureName, structureVersion);

        /// <summary>
        ///     preview the Result of building the given Environment and Structure
        /// </summary>
        /// <param name="previewOptions"></param>
        /// <returns></returns>
        [HttpPost(Name = ApiVersionFormatted + "PreviewConfigurationWithGivenValues")]
        public async Task<IActionResult> PreviewConfiguration([FromBody] PreviewContainer previewOptions)
        {
            if (previewOptions is null)
                return BadRequest("no preview-data received");

            if (previewOptions.Environment is null)
                return BadRequest("no environment-data received");

            if (previewOptions.Structure is null)
                return BadRequest("no structure-data received");

            var environmentInfo = new EnvironmentCompilationInfo
            {
                Name = "Intermediate-Preview-Environment",
                Keys = await ResolveEnvironmentPreview(previewOptions.Environment)
            };

            var (structKeys, varKeys) = await ResolveStructurePreview(previewOptions.Structure);
            var structureInfo = new StructureCompilationInfo
            {
                Name = "Intermediate-Preview-Structure",
                Keys = structKeys,
                Variables = varKeys
            };

            var compiled = _compiler.Compile(environmentInfo,
                                             structureInfo,
                                             _parser);

            var json = _translator.ToJson(compiled.CompiledConfiguration);

            return Ok(new V0.PreviewResult
            {
                Map = compiled.CompiledConfiguration.ToImmutableSortedDictionary(),
                Json = json,
                UsedKeys = compiled.GetUsedKeys().Where(key => environmentInfo.Keys.ContainsKey(key))
            });
        }

        private async Task<IDictionary<string, string>> ResolveEnvironmentPreview(EnvironmentPreview environment)
        {
            // if there is a reference to an existing Environment, retrieve its keys
            if (!string.IsNullOrWhiteSpace(environment.Category) && !string.IsNullOrWhiteSpace(environment.Name))
            {
                var id = new EnvironmentIdentifier(environment.Category, environment.Name);
                var result = await _store.Environments.GetKeys(id, QueryRange.All);

                if (!result.IsError)
                    return result.Data;

                Logger.LogWarning($"could not retrieve referenced environment '{id}' for preview: {result.Message}");
            }

            // if no reference or couldn't retrieve keys - fall back to custom-keys
            return environment.Keys ?? new Dictionary<string, string>();
        }

        private async Task<(IDictionary<string, string> Structure, IDictionary<string, string> Variables)> ResolveStructurePreview(StructurePreview structure)
        {
            if (!string.IsNullOrWhiteSpace(structure.Name) && !string.IsNullOrWhiteSpace(structure.Version))
            {
                int.TryParse(structure.Version, out var structureVersion);
                var id = new StructureIdentifier(structure.Name, structureVersion);

                var structResult = await _store.Structures.GetKeys(id, QueryRange.All);
                var variableResult = await _store.Structures.GetVariables(id, QueryRange.All);

                if (!structResult.IsError && !variableResult.IsError)
                    return (Structure: structResult.Data, Variables: variableResult.Data);

                Logger.LogWarning($"could not retrieve referenced structure / variabels '{id}' for preview: " +
                    $"{(structResult.IsError ? structResult.Message : variableResult.Message)}");
            }

            return (Structure: structure.Keys ?? new Dictionary<string, string>(),
                Variables: structure.Variables ?? new Dictionary<string, string>());
        }

        /// <summary>
        ///     Container for data that should be previewed
        /// </summary>
        public class PreviewContainer
        {
            /// <inheritdoc cref="EnvironmentPreview" />
            public EnvironmentPreview Environment { get; set; }

            /// <inheritdoc cref="StructurePreview" />
            public StructurePreview Structure { get; set; }
        }

        /// <summary>
        ///     Reference to an existing Environment, or custom Keys
        /// </summary>
        public class EnvironmentPreview
        {
            /// <summary>
            ///     Reference to an existing Environment
            /// </summary>
            public string Category { get; set; }

            /// <summary>
            ///     Reference to an existing Environment
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Custom Keys
            /// </summary>
            public Dictionary<string, string> Keys { get; set; }
        }

        /// <summary>
        ///     Reference to an existing Structure, or custom Keys
        /// </summary>
        public class StructurePreview
        {
            /// <summary>
            ///     Reference to an existing Structure
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Reference to an existing Structure
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            ///     Custom Keys
            /// </summary>
            public Dictionary<string, string> Keys { get; set; }

            /// <summary>
            ///     Custom Variables
            /// </summary>
            public Dictionary<string, string> Variables { get; set; }
        }
    }
}