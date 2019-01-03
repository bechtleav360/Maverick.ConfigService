using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     preview the Results of Building different Configurations
    /// </summary>
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

        /// <summary>
        ///     preview the Result of building an existing Environment and a Structure
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <param name="structureName"></param>
        /// <param name="structureVersion"></param>
        /// <returns></returns>
        [HttpGet("{environmentCategory}/{environmentName}/{structureName}/{structureVersion}")]
        public async Task<IActionResult> PreviewConfiguration([FromRoute] string environmentCategory,
                                                              [FromRoute] string environmentName,
                                                              [FromRoute] string structureName,
                                                              [FromRoute] int structureVersion)
        {
            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);
            var structId = new StructureIdentifier(structureName, structureVersion);

            var structureKeyResult = await _store.Structures.GetKeys(structId);
            if (structureKeyResult.IsError)
                throw new Exception(structureKeyResult.Message);

            var structureVariableResult = await _store.Structures.GetVariables(structId);
            if (structureVariableResult.IsError)
                throw new Exception(structureVariableResult.Message);

            var environmentResult = await _store.Environments.GetKeys(envId);
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
        [HttpPost]
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

            var traceResults = new List<KeyTraceResult>();
            var traceStack = new Stack<TraceResult>();

            foreach (var trace in compiled.CompilationTrace)
                traceStack.Push(trace);

            while (traceStack.TryPop(out var trace))
            {
                foreach (var _ in trace.Children)
                    traceStack.Push(_);

                if (trace is KeyTraceResult keyTrace)
                    traceResults.Add(keyTrace);
            }

            var x = traceResults.Where(k => environmentInfo.Keys.ContainsKey(k.Key))
                                .GroupBy(r => r.Key)
                                .Select(g => g.First())
                                .ToArray();

            var stack = new Stack<TraceResult>();
            foreach (var item in x)
                stack.Push(item);

            var usedKeys = new List<string>();

            while (stack.TryPop(out var r))
            {
                if (r is KeyTraceResult k)
                    usedKeys.Add(k.Key);

                foreach (var c in r.Children)
                    stack.Push(c);
            }

            return Ok(new PreviewResult
            {
                Map = compiled.CompiledConfiguration,
                Json = json,
                UsedKeys = usedKeys.GroupBy(_ => _)
                                   .Select(_ => _.Key)
                                   .OrderBy(_ => _)
                                   .ToList()
            });
        }
    }

    /// <summary>
    /// </summary>
    public class PreviewContainer
    {
        /// <summary>
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }

        /// <summary>
        /// </summary>
        public Dictionary<string, string> Structure { get; set; }

        /// <summary>
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }

    /// <summary>
    /// </summary>
    public class PreviewResult
    {
        /// <summary>
        /// </summary>
        public IDictionary<string, string> Map { get; set; }

        /// <summary>
        /// </summary>
        public JToken Json { get; set; }

        /// <summary>
        /// </summary>
        public List<string> UsedKeys { get; set; }
    }
}