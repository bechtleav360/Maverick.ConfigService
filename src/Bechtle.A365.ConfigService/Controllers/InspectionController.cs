using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers
{
    /// <summary>
    ///     controller to inspect and analyze the current state of all stored Configuration-Data
    /// </summary>
    [Route("inspect")]
    [ApiVersion(ApiVersions.V1, Deprecated = ApiDeprecation.V1)]
    public class InspectionController : ControllerBase
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IProjectionStore _store;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public InspectionController(IServiceProvider provider,
                                    ILogger<InspectionController> logger,
                                    IConfigurationCompiler compiler,
                                    IConfigurationParser parser,
                                    IJsonTranslator translator,
                                    IProjectionStore store)
            : base(provider, logger)
        {
            _compiler = compiler;
            _parser = parser;
            _translator = translator;
            _store = store;
        }

        // TODO: find a better name, this one is horrible
        /// <summary>
        ///     annotate each key in the current Environment with the structures that used each key to build.
        ///     basically this show which keys are used in which version, and which aren't used at all
        /// </summary>
        /// <returns></returns>
        [HttpPost("environment/{environmentCategory}/{environmentName}/structures/all")]
        public async Task<IActionResult> GetUsedKeysPerStructureAll([FromRoute] string environmentCategory,
                                                                    [FromRoute] string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentCategory))
                return BadRequest("no environment-category given");

            if (string.IsNullOrWhiteSpace(environmentName))
                return BadRequest("no environment-name given");

            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);

            var envKeyResult = await _store.Environments.GetKeys(envId, QueryRange.All);

            if (envKeyResult.IsError)
                return ProviderError(envKeyResult);

            var envKeys = envKeyResult.Data;

            var configResult = await _store.Configurations.GetAvailableWithEnvironment(envId, DateTime.MinValue, QueryRange.All);

            if (configResult.IsError)
                return ProviderError(configResult);

            return Result(await AnnotateEnvironmentKeys(envKeys, configResult.Data));
        }

        /// <summary>
        ///     use the most up-to-date structures to find unused keys in an environment
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <returns></returns>
        [HttpPost("environment/{environmentCategory}/{environmentName}/structures/latest")]
        public async Task<IActionResult> GetUsedKeysPerStructureLatest([FromRoute] string environmentCategory,
                                                                       [FromRoute] string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentCategory))
                return BadRequest("no environment-category given");

            if (string.IsNullOrWhiteSpace(environmentName))
                return BadRequest("no environment-name given");

            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);

            var envKeyResult = await _store.Environments.GetKeys(envId, QueryRange.All);

            if (envKeyResult.IsError)
                return ProviderError(envKeyResult);

            var envKeys = envKeyResult.Data;

            var configResult = await _store.Configurations.GetAvailableWithEnvironment(envId, DateTime.MinValue, QueryRange.All);

            if (configResult.IsError)
                return ProviderError(configResult);

            return Result(await AnnotateEnvironmentKeys(envKeys,
                                                        configResult.Data
                                                                    .GroupBy(c => c.Structure.Name)
                                                                    .Select(g => g.OrderByDescending(c => c.Structure.Version).First())));
        }

        /// <summary>
        ///     inspect the given structure for possible errors or unreachable references
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <returns></returns>
        [HttpPost("structure/compile/{environmentCategory}/{environmentName}")]
        public async Task<IActionResult> InspectStructure([FromRoute] string environmentCategory,
                                                          [FromRoute] string environmentName,
                                                          [FromBody] DtoStructure structure)
        {
            if (string.IsNullOrWhiteSpace(environmentCategory))
                return BadRequest("no environment-category given");

            if (string.IsNullOrWhiteSpace(environmentName))
                return BadRequest("no environment-name given");

            if (structure is null)
                return BadRequest("no structure was uploaded");

            if (structure.Structure is null)
                return BadRequest("structure doesn't contain a body ($.Structure)");

            var envId = new EnvironmentIdentifier(environmentCategory, environmentName);

            var envKeysResult = await _store.Environments.GetKeys(envId, QueryRange.All);

            if (envKeysResult.IsError)
                return ProviderError(envKeysResult);

            var envKeys = envKeysResult.Data;

            IDictionary<string, string> structKeys;

            try
            {
                structKeys = _translator.ToDictionary(structure.Structure);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "could not translate given json.Structure to dictionary");
                return BadRequest("structure could not be mapped to a dictionary ($.Structure)");
            }

            CompilationResult compilationResult;

            try
            {
                compilationResult = _compiler.Compile(new EnvironmentCompilationInfo
                                                      {
                                                          Keys = envKeys,
                                                          Name = envId.ToString()
                                                      }, new StructureCompilationInfo
                                                      {
                                                          Name = structure.Name ?? "Inspected Structure",
                                                          Keys = structKeys,
                                                          Variables = structure.Variables ?? new Dictionary<string, string>()
                                                      },
                                                      _parser);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"structure could not be inspected in context of '{envId}'; compilation failed");
                return Ok(new StructureInspectionResult
                {
                    CompilationSuccessful = false
                });
            }

            var result = AnalyzeCompilation(compilationResult);

            return Ok(result);
        }

        private StructureInspectionResult AnalyzeCompilation(CompilationResult compilationResult)
        {
            var traceList = new List<TraceResult>(compilationResult.CompiledConfiguration.Count);
            var stack = new Stack<TraceResult>();
            // prepare stack with initial data
            foreach (var item in compilationResult.CompilationTrace)
                stack.Push(item);
            while (stack.TryPop(out var item))
            {
                traceList.Add(item);
                foreach (var child in item.Children)
                    stack.Push(child);
            }

            var warnings = new Dictionary<string, List<string>>();
            foreach (var traceResult in traceList.OfType<KeyTraceResult>())
            foreach (var warning in traceResult.Warnings)
            {
                if (!warnings.ContainsKey(traceResult.Key))
                    warnings.Add(traceResult.Key, new List<string>());
                warnings[traceResult.Key].Add(warning);
            }

            var errors = new Dictionary<string, List<string>>();
            foreach (var traceResult in traceList.OfType<KeyTraceResult>())
            foreach (var error in traceResult.Errors)
            {
                if (!errors.ContainsKey(traceResult.Key))
                    errors.Add(traceResult.Key, new List<string>());
                errors[traceResult.Key].Add(error);
            }

            var result = new StructureInspectionResult
            {
                CompiledConfiguration = compilationResult.CompiledConfiguration,
                CompilationSuccessful = true,
                Stats = new CompilationStats
                {
                    ReferencesUsed = traceList.OfType<MultiTraceResult>().Count(),
                    StaticValuesUsed = traceList.OfType<ValueTraceResult>().Count()
                },
                Warnings = warnings,
                Errors = errors
            };

            return result;
        }

        private async Task<IResult<List<AnnotatedEnvironmentKey>>> AnnotateEnvironmentKeys(IDictionary<string, string> environment,
                                                                                           IEnumerable<ConfigurationIdentifier> structures)
        {
            var tasks = structures.AsParallel()
                                  .Select(cid => (ConfigId: cid, Task: _store.Configurations
                                                                             .GetUsedConfigurationKeys(cid,
                                                                                                       DateTime.MinValue,
                                                                                                       QueryRange.All)))
                                  .ToList();

            await Task.WhenAll(tasks.Select(t => t.Task));

            var results = tasks.Select(t => (t.ConfigId, t.Task.Result)).ToList();
            var errors = results.Where(r => r.Result.IsError).ToList();

            if (errors.Any(r => r.Result.IsError))
                return Common.Result.Error<List<AnnotatedEnvironmentKey>>(errors.First().Result.Message, errors.First().Result.Code);

            var annotatedEnv = new List<AnnotatedEnvironmentKey>(environment.Select(kvp => new AnnotatedEnvironmentKey
            {
                Key = kvp.Key,
                Value = kvp.Value
            }));

            // go through each built configuration, and annotate the used Environment-Keys with their Structure-Id
            // at the end we have a list of Keys and Values, each with a list of structures that used this key
            results.AsParallel()
                   .ForAll(t => t.Result
                                 .Data
                                 .AsParallel()
                                 .ForAll(key => annotatedEnv.FirstOrDefault(a => a.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                                                            ?.Structures
                                                            .Add(t.ConfigId.Structure)));

            return Common.Result.Success(annotatedEnv);
        }
    }
}