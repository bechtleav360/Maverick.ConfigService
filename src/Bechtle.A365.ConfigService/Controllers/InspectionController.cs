﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Dto;
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

        /// <summary>
        ///     use the most up-to-date structures to find unused keys in an environment
        /// </summary>
        /// <param name="environmentCategory"></param>
        /// <param name="environmentName"></param>
        /// <returns></returns>
        [HttpPost("environment/{environmentCategory}/{environmentName}/structures/latest")]
        public async Task<IActionResult> FindUnusedEnvironmentKeys([FromRoute] string environmentCategory,
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

            var configIds = configResult.Data
                                        .GroupBy(c => c.Structure.Name)
                                        .Select(g => g.OrderByDescending(c => c.Structure.Version).First())
                                        .ToList();

            var tasks = configIds.AsParallel()
                                 .Select(cid => _store.Configurations.GetUsedConfigurationKeys(cid, DateTime.MinValue, QueryRange.All))
                                 .ToList();

            await Task.WhenAll(tasks);

            var results = tasks.Select(t => t.Result).ToList();
            var errors = results.Where(r => r.IsError).ToList();

            if (errors.Any(r => r.IsError))
                return ProviderError(errors.First());

            // select all used keys from all queried configurations into a flat list
            var usedKeys = results.SelectMany(r => r.Data)
                                  .GroupBy(p => p)
                                  .Select(g => g.Key)
                                  .ToList();

            var unusedKeys = new Dictionary<string, string>();
            foreach (var (key, value) in envKeys)
            {
                if (usedKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                unusedKeys.Add(key, value);
            }

            return Ok(_translator.ToJson(unusedKeys));
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
    }

    /// <summary>
    ///     Details about the Compilation of a Structure with an Environment
    /// </summary>
    public class StructureInspectionResult
    {
        /// <summary>
        ///     flag indicating if the compilation was successful or not
        /// </summary>
        public bool CompilationSuccessful { get; set; }

        /// <summary>
        ///     resulting compiled configuration
        /// </summary>
        public IDictionary<string, string> CompiledConfiguration { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///     Path => Error dictionary
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; set; }

        /// <inheritdoc cref="CompilationStats" />
        public CompilationStats Stats { get; set; } = new CompilationStats();

        /// <summary>
        ///     Path => Warning dictionary
        /// </summary>
        public Dictionary<string, List<string>> Warnings { get; set; }
    }

    /// <summary>
    ///     stats about the compilation (ref-counts, hits / misses, etc)
    /// </summary>
    public class CompilationStats
    {
        /// <summary>
        ///     number of used References
        /// </summary>
        public int ReferencesUsed { get; set; }

        /// <summary>
        ///     number of used Static values
        /// </summary>
        public int StaticValuesUsed { get; set; }
    }
}