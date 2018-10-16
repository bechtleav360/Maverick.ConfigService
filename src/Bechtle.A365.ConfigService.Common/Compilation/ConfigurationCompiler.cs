using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        private const int KeyRecursionLimit = 100;

        private readonly ILogger<ConfigurationCompiler> _logger;

        public ConfigurationCompiler(ILogger<ConfigurationCompiler> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> Compile(EnvironmentCompilationInfo environment,
                                                               StructureCompilationInfo structure,
                                                               IConfigurationParser parser)
        {
            var context = new CompilationContext
            {
                CurrentKey = string.Empty,
                StructureInfo = structure,
                EnvironmentInfo = environment,
                Parser = parser
            };

            foreach (var kvp in structure.Keys)
            {
                context.CurrentKey = kvp.Key;

                var result = await CompileInternal(context, kvp.Value);
                if (!result.Any()) context.AddError(context.CurrentKey, "can't compile, no results found");

                foreach (var (key, value) in result)
                    context.Result[key] = value;
            }

            return context.Result;
        }

        /// <summary>
        ///     analyze the given parts and determine if they can be compiled and what would be the result
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        private async Task<CompilationPlan> AnalyzeCompilation(IList<ConfigValuePart> parts)
        {
            await Task.Yield();

            // no compilation possible because there is nothing to compile - Compiler should insert string.Empty
            if (!parts.Any())
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    Reason = "nothing to compile"
                };

            // simplest possible plan - no references and only values
            if (parts.All(p => p is ValuePart))
                return new CompilationPlan
                {
                    CompilationPossible = true,
                    Reason = string.Empty
                };

            var valueParts = parts.OfType<ValuePart>()
                                  .ToArray();

            var pathReferences = parts.OfType<ReferencePart>()
                                      .Where(p => p.Commands.ContainsKey(ReferenceCommand.Path))
                                      .ToArray();

            var regionReferences = pathReferences.Where(p => p.Commands[ReferenceCommand.Path].EndsWith('*'))
                                                 .ToArray();

            // can't compile something that references multiple regions at the same time
            if (regionReferences.Length > 1)
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    Reason = "multiple region-spanning references found"
                };

            // can't reference a region when other stuff would be discarded
            if (regionReferences.Any() && valueParts.Any())
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    Reason = "region-spanning reference found within non-reference text"
                };

            // can't compile something that references a region and keys at the same time
            // at least some stuff would be discarded, and that does not qualify as a valid compilation
            if (pathReferences.Length > 1 &&
                pathReferences.Any(p => p.Commands[ReferenceCommand.Path].EndsWith('*')))
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    Reason = "region-spanning and value-only references found"
                };

            return new CompilationPlan
            {
                CompilationPossible = true,
                Reason = string.Empty
            };
        }

        private async Task<(string Key, string Value)[]> CompileInternal(CompilationContext context, string value)
        {
            if (context.RecursionLevel > KeyRecursionLimit)
                return new (string Key, string Value)[0];

            var parts = context.Parser.Parse(value);

            var plan = await AnalyzeCompilation(parts);

            if (!plan.CompilationPossible)
            {
                _logger.LogError($"can't compile key '{context.CurrentKey}': {plan.Reason}");
                context.AddError(context.CurrentKey, $"can't compile: {plan.Reason}");
                return new (string Key, string Value)[0];
            }

            var valueBuilder = new StringBuilder();

            foreach (var part in parts)
            {
                if (part is ValuePart valuePart)
                {
                    valueBuilder.Append(valuePart.Text);
                    continue;
                }

                if (!(part is ReferencePart reference))
                {
                    _logger.LogError($"unknown part parsed during Config-Compilation '{part.GetType().Name}'");
                    continue;
                }

                // inspect reference and add alias to context if possible
                await HandleAliasCommands(context, reference);

                // if we don't have a path to substitute and only modify the context we can continue
                if (!reference.Commands.ContainsKey(ReferenceCommand.Path))
                    continue;

                // inspect reference and get current Path and Repository
                // Repository can be changed by accessing different aliases
                var (path, repository) = await HandlePathCommands(context, reference);

                if (path.EndsWith("*"))
                    return await HandleRegionSelection(context, repository, path);

                // now comes the part where we handle a single compiled result...
                // actually match
                var match = repository.Keys.FirstOrDefault(key => key.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    _logger.LogError($"could not resolve path '{path}'");
                    context.AddError(context.CurrentKey, $"could not resolve path '{path}'");
                    continue;
                }

                // compile the matched result until it's done
                var innerContext = new CompilationContext(context) {CurrentKey = path};
                innerContext.IncrementRecursionLevel();

                var result = await CompileInternal(innerContext, repository[match]);

                context.CarryOverErrors(innerContext);

                // if the result doesn't contain anything we just go on with our lives...
                // if it turns out the compiled reference does actually point to a region we need to handle it as such and return immediately
                if (result.Length > 1)
                    return result.Select(item => (Key: item.Key, Value: item.Value))
                                 .ToArray();

                // otherwise we can carry on and add the value to our result
                if (result.Length == 1)
                    valueBuilder.Append(result.First().Value);
            }

            return new[] {(Key: context.CurrentKey, Value: valueBuilder.ToString())};
        }

        /// <summary>
        ///     inspect the given ReferencePart and add an Alias if possible
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private async Task HandleAliasCommands(CompilationContext context, ReferencePart reference)
        {
            await Task.Yield();

            var commands = reference.Commands;
            if (commands.ContainsKey(ReferenceCommand.Alias) && commands.ContainsKey(ReferenceCommand.Using))
            {
                // add the alias to the context
                var alias = commands[ReferenceCommand.Alias];
                var @using = commands[ReferenceCommand.Using];

                _logger.LogTrace($"adding alias '{alias}' => '{@using}' to the context");
                context.ReferenceAliases[alias] = @using;
            }
            else if (!commands.ContainsKey(ReferenceCommand.Alias) && !commands.ContainsKey(ReferenceCommand.Using))
            {
                // don't actually care, but this makes it clear when the next section of code runs
            }
            else
            {
                _logger.LogError($"either {nameof(ReferenceCommand.Alias)} or {nameof(ReferenceCommand.Using)} is set, " +
                                 "but not both - they are only usable together");
                context.AddError(context.CurrentKey, $"either {nameof(ReferenceCommand.Alias)} or {nameof(ReferenceCommand.Using)} is set, " +
                                                     "but not both - they are only usable together");
            }
        }

        private async Task<(string Path, IDictionary<string, string> Repository)> HandlePathCommands(CompilationContext context, ReferencePart reference)
        {
            await Task.Yield();

            // default path = path given without de-referencing aliases and the like
            var path = reference.Commands[ReferenceCommand.Path];

            // default repository is Environment
            var repository = context.EnvironmentInfo.Keys;

            if (path.StartsWith('$'))
            {
                var split = path.Split('/', 2);
                var alias = split[0].TrimStart('$');
                var rest = split[1];

                if (context.ReferenceAliases.ContainsKey(alias))
                {
                    path = context.ReferenceAliases[alias] + "/" + rest;
                }
                else if (alias.Equals("this", StringComparison.OrdinalIgnoreCase))
                {
                    path = $"{context.CurrentKey.Substring(0, context.CurrentKey.LastIndexOf('/'))}/{rest}";
                }
                else if (alias.Equals("struct", StringComparison.OrdinalIgnoreCase))
                {
                    path = rest;
                    repository = context.StructureInfo.Variables;
                }
                else
                {
                    _logger.LogError($"could not resolve alias '{alias}' in '{context.CurrentKey}'");
                    context.AddError(context.CurrentKey, $"could not resolve alias '{alias}'");
                }
            }

            return (Path: path, Repository: repository);
        }

        private async Task<(string Key, string Value)[]> HandleRegionSelection(CompilationContext context,
                                                                               IDictionary<string, string> repository,
                                                                               string path)
        {
            var pathMatcher = path.TrimEnd('*');
            var matchedItems = repository.Keys
                                         .Where(key => key.StartsWith(pathMatcher, StringComparison.OrdinalIgnoreCase))
                                         .Select(key => (Key: key, Value: repository[key]))
                                         .ToArray();

            // recursion to resolve keys within the selected region
            var resultItems = new Dictionary<string, string>(matchedItems.Length);
            foreach (var (itemKey, itemValue) in matchedItems)
            {
                var regionContext = new CompilationContext(context) {CurrentKey = itemKey};
                regionContext.IncrementRecursionLevel();

                var regionResult = await CompileInternal(regionContext, itemValue);

                // carry errors encountered in region-compilation with us
                foreach (var errorGroup in regionContext.Errors)
                foreach (var error in errorGroup.Value)
                    context.AddError(errorGroup.Key, error);

                foreach (var (k, v) in regionResult)
                    resultItems[k] = v;
            }

            // resultItems should be fully resolved
            return resultItems.Select(item => (Key: item.Key, Value: item.Value))
                              .ToArray();
        }

        private struct CompilationPlan
        {
            public bool CompilationPossible { get; set; }

            public string Reason { get; set; }
        }

        private class CompilationContext
        {
            /// <inheritdoc />
            public CompilationContext()
            {
            }

            /// <inheritdoc />
            public CompilationContext(CompilationContext context)
            {
                EnvironmentInfo = context.EnvironmentInfo;
                StructureInfo = context.StructureInfo;
                RecursionLevel = context.RecursionLevel;
                Result = context.Result;
                CurrentKey = context.CurrentKey;
                Parser = context.Parser;
            }

            public string CurrentKey { get; set; } = string.Empty;

            public EnvironmentCompilationInfo EnvironmentInfo { get; set; }

            public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            public IConfigurationParser Parser { get; set; }

            public int RecursionLevel { get; private set; }

            public Dictionary<string, string> ReferenceAliases { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string> Result { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public StructureCompilationInfo StructureInfo { get; set; }

            public void AddError(string key, string errorMessage)
            {
                if (!Errors.ContainsKey(key))
                    Errors[key] = new List<string>();
                Errors[key].Add(errorMessage);
            }

            public void CarryOverErrors(CompilationContext context)
            {
                // carry errors encountered in region-compilation with us
                foreach (var errorGroup in context.Errors)
                foreach (var error in errorGroup.Value)
                    AddError(errorGroup.Key, error);
            }

            public void IncrementRecursionLevel() => RecursionLevel += 1;
        }
    }
}