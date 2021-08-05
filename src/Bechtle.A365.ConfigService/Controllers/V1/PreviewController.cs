﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     preview the Results of Building different Configurations
    /// </summary>
    [Route(ApiBaseRoute + "preview")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class PreviewController : ControllerBase
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public PreviewController(
            ILogger<PreviewController> logger,
            IConfigurationCompiler compiler,
            IProjectionStore store,
            IConfigurationParser parser,
            IJsonTranslator translator)
            : base(logger)
        {
            _compiler = compiler;
            _store = store;
            _parser = parser;
            _translator = translator;
        }

        /// <summary>
        ///     preview the Result of building an existing Environment and a Structure
        /// </summary>
        /// <param name="environmentCategory">Environment-Category to use for this Preview</param>
        /// <param name="environmentName">Environment-Name to use for this Preview</param>
        /// <param name="structureName">Structure-Name to use for this Preview</param>
        /// <param name="structureVersion">Structure-Version to use for this Preview</param>
        /// <returns>built configuration rendered as JSON</returns>
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}", Name = "PreviewConfigurationWithStoredValues")]
        public async Task<IActionResult> PreviewConfiguration(
            [FromRoute] string environmentCategory,
            [FromRoute] string environmentName,
            [FromRoute] string structureName,
            [FromRoute] int structureVersion)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structId = new StructureIdentifier(structureName, structureVersion);

            var structureKeyResult = await _store.Structures.GetKeys(structId, QueryRange.All);
            if (structureKeyResult.IsError)
                return ProviderError(structureKeyResult);

            var structureVariableResult = await _store.Structures.GetVariables(structId, QueryRange.All);
            if (structureVariableResult.IsError)
                return ProviderError(structureVariableResult);

            var environmentResult = await _store.Environments.GetKeys(
                                        new KeyQueryParameters<EnvironmentIdentifier>
                                        {
                                            Identifier = envId,
                                            Range = QueryRange.All
                                        });

            if (environmentResult.IsError)
                return ProviderError(environmentResult);

            var structureSnapshot = new Dictionary<string, string>(structureKeyResult.Data.Items);
            var variableSnapshot = new Dictionary<string, string>(structureVariableResult.Data.Items);
            var environmentSnapshot = new Dictionary<string, string>(environmentResult.Data.Items);

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

            try
            {
                var compiled = _compiler.Compile(
                    environmentInfo,
                    structureInfo,
                    _parser);

                var json = _translator.ToJson(compiled.CompiledConfiguration);

                return Ok(json);
            }
            catch (Exception e)
            {
                KnownMetrics.Exception.WithLabels(e.GetType().Name).Inc();
                Logger.LogError(
                    e,
                    "failed to add new environment at ("
                    + $"{nameof(environmentCategory)}: {environmentCategory}; "
                    + $"{nameof(environmentName)}: {environmentName}; "
                    + $"{nameof(structureName)}: {structureName}; "
                    + $"{nameof(structureVersion)}: {structureVersion})");
                return Ok(JsonDocument.Parse("{}").RootElement);
            }
        }

        /// <summary>
        ///     preview the Result of building the given Environment and Structure
        /// </summary>
        /// <param name="previewOptions">set of options telling the service which existing Env/Structure to use, or to use inline-data for preview</param>
        /// <returns>Result of the Compilation with associated Metadata and both Key/Value and JSON representation of the Config</returns>
        [ProducesResponseType(typeof(PreviewResult), (int)HttpStatusCode.OK)]
        [HttpPost(Name = "PreviewConfigurationWithGivenValues")]
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

            var compiled = _compiler.Compile(
                environmentInfo,
                structureInfo,
                _parser);

            return Ok(
                new PreviewResult
                {
                    Map = compiled.CompiledConfiguration.ToDictionary(_ => _.Key, _ => _.Value),
                    Json = _translator.ToJson(compiled.CompiledConfiguration),
                    UsedKeys = compiled.GetUsedKeys().Where(key => environmentInfo.Keys.ContainsKey(key))
                });
        }

        private async Task<IDictionary<string, string>> ResolveEnvironmentPreview(EnvironmentPreview environment)
        {
            // if there is a reference to an existing Environment, retrieve its keys
            if (!string.IsNullOrWhiteSpace(environment.Category) && !string.IsNullOrWhiteSpace(environment.Name))
            {
                var id = new EnvironmentIdentifier(environment.Category, environment.Name);
                var result = await _store.Environments.GetKeys(
                                 new KeyQueryParameters<EnvironmentIdentifier>
                                 {
                                     Identifier = id,
                                     Range = QueryRange.All
                                 });

                if (!result.IsError)
                    return new Dictionary<string, string>(result.Data.Items);

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
                    return (Structure: new Dictionary<string, string>(structResult.Data.Items),
                               Variables: new Dictionary<string, string>(variableResult.Data.Items));

                Logger.LogWarning(
                    $"could not retrieve referenced structure / variables '{id}' for preview: "
                    + $"{(structResult.IsError ? structResult.Message : variableResult.Message)}");
            }

            return (Structure: structure.Keys?.ToDictionary(
                                   pair => pair.Key,
                                   pair => pair.Value?.ToString())
                               ?? new Dictionary<string, string>(),
                       Variables: structure.Variables?.ToDictionary(
                                      pair => pair.Key,
                                      pair => pair.Value?.ToString())
                                  ?? new Dictionary<string, string>());
        }
    }
}
