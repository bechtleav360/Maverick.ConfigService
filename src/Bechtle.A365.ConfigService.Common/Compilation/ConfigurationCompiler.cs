using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Tracers;
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

        // basically, do the compilation for each key in parallel - as parallel as PLINQ wants it to be
        // then collect the results and convert it to a dictionary
        /// <inheritdoc />
        public CompilationResult Compile(EnvironmentCompilationInfo environment,
                                         StructureCompilationInfo structure,
                                         IConfigurationParser parser)
        {
            ICompilationTracer compilationTracer = new CompilationTracer();
            var configuration = structure.Keys
                                         .AsParallel()
                                         .SelectMany(kvp => CompileInternal(new CompilationContext
                                                                            {
                                                                                Tracer = compilationTracer.AddKey(kvp.Key, kvp.Value),
                                                                                CurrentKey = kvp.Key,
                                                                                StructureInfo = structure,
                                                                                EnvironmentInfo = environment,
                                                                                Parser = parser
                                                                            },
                                                                            kvp.Value))
                                         .ToArray()
                                         .ToDictionary(t => t.Key, t => t.Value);

            return new CompilationResult(configuration, compilationTracer.GetResults());
        }

        /// <summary>
        ///     used to log messages with the current CompilationContext
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string WithContext(CompilationContext context, string message)
            => $"'{context.EnvironmentInfo.Name}' / '{context.StructureInfo.Name}' / '{context.CurrentKey}' {message}";

        /// <summary>
        ///     analyze the given parts and determine if they can be compiled and what would be the result
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        private (bool CompilationPossible, string Reason) AnalyzeCompilation(CompilationContext context,
                                                                             IList<ConfigValuePart> parts)
        {
            _logger.LogTrace(WithContext(context, "analyzing compilation"));

            // no compilation possible / needed, but still a valid value
            // simplest possible plan - no references and only values
            if (!parts.Any() || parts.All(p => p is ValuePart))
                return (CompilationPossible: true, Reason: string.Empty);

            var valueParts = parts.OfType<ValuePart>()
                                  .ToArray();

            var pathReferences = parts.OfType<ReferencePart>()
                                      .Where(p => p.Commands.ContainsKey(ReferenceCommand.Path))
                                      .ToArray();

            var regionReferences = pathReferences.Where(p => p.Commands[ReferenceCommand.Path].EndsWith('*'))
                                                 .ToArray();

            // can't compile something that references multiple regions at the same time
            if (regionReferences.Length > 1)
                return (CompilationPossible: false, Reason: "multiple region-spanning references found");

            // can't reference a region when other stuff would be discarded
            if (regionReferences.Any() && valueParts.Any())
                return (CompilationPossible: false, Reason: "region-spanning reference found within non-reference text");

            // can't compile something that references a region and keys at the same time
            // at least some stuff would be discarded, and that does not qualify as a valid compilation
            if (pathReferences.Length > 1 &&
                pathReferences.Any(p => p.Commands[ReferenceCommand.Path].EndsWith('*')))
                return (CompilationPossible: false, Reason: "region-spanning and value-only references found");

            return (CompilationPossible: true, Reason: string.Empty);
        }

        private (string Key, string Value)[] CompileInternal(CompilationContext context, string value)
        {
            _logger.LogTrace(WithContext(context, $"compiling key, recursion: '{context.RecursionLevel}'"));

            if (string.IsNullOrEmpty(value))
                return new[] {(context.CurrentKey, string.Empty)};

            if (context.RecursionLevel > KeyRecursionLimit)
            {
                _logger.LogWarning(WithContext(context, "recursion too deep, aborting compilation"));
                return new (string Key, string Value)[0];
            }

            _logger.LogTrace(WithContext(context, "parsing value for references"));
            var parts = context.Parser.Parse(value);

            var plan = AnalyzeCompilation(context, parts);

            if (!plan.CompilationPossible)
            {
                _logger.LogWarning(WithContext(context, $"can't compile key: {plan.Reason}"));
                return new[] {(Key: context.CurrentKey, Value: value)};
            }

            var valueBuilder = new StringBuilder();

            foreach (var part in parts)
            {
                if (part is ValuePart valuePart)
                {
                    context.Tracer.AddStaticValue(valuePart.Text);
                    valueBuilder.Append(valuePart.Text);
                    continue;
                }

                if (!(part is ReferencePart reference))
                {
                    _logger.LogError($"unknown part parsed: '{part.GetType().Name}'");
                    continue;
                }

                HandleAliasCommands(context, reference);

                // if we don't have a path to substitute and only modify the context we can continue
                if (!reference.Commands.ContainsKey(ReferenceCommand.Path))
                    continue;

                var (path, repository) = HandlePathCommands(context, reference);

                if (path.EndsWith("*"))
                    return HandleRegionSelection(context, repository, path);

                // now comes the part where we handle a single compiled result...
                var match = repository.Keys.FirstOrDefault(key => key.Equals(path, StringComparison.OrdinalIgnoreCase));
                string matchedValue;

                if (!(match is null))
                {
                    matchedValue = repository[match];
                }
                else
                {
                    // if 'Fallback' or 'Default' are set in the reference,
                    // we can use the it instead of the value we're searching for
                    if (reference.Commands.ContainsKey(ReferenceCommand.Fallback))
                    {
                        var fallbackValue = reference.Commands[ReferenceCommand.Fallback];
                        _logger.LogWarning($"could not resolve path '{path}'");
                        _logger.LogInformation($"using fallback '{fallbackValue}' after failing to resolve '{path}'");
                        matchedValue = fallbackValue;
                    }
                    else
                    {
                        _logger.LogError($"could not resolve path '{path}'");
                        continue;
                    }
                }

                // compile the matched result until it's done
                var matchTracer = context.Tracer.AddPathResolution(path);
                var innerContext = new CompilationContext(context)
                {
                    CurrentKey = path,
                    Tracer = matchTracer.AddPathResult(matchedValue)
                };
                innerContext.IncrementRecursionLevel();

                var result = CompileInternal(innerContext, matchedValue);

                // if the result doesn't contain anything we just go on with our lives...
                // if it turns out the compiled reference does actually point to a region we need to handle it as such and return immediately
                if (result.Length > 1)
                {
                    _logger.LogWarning(WithContext(context, $"reference '{path}' pointed towards single value, but was resolved to a region"));
                    return result.Select(item => (item.Key, item.Value)).ToArray();
                }

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
        private void HandleAliasCommands(CompilationContext context, ReferencePart reference)
        {
            var commands = reference.Commands;
            if (commands.ContainsKey(ReferenceCommand.Alias) && commands.ContainsKey(ReferenceCommand.Using))
            {
                // add the alias to the context
                var alias = commands[ReferenceCommand.Alias];
                var @using = commands[ReferenceCommand.Using];

                _logger.LogTrace(WithContext(context, $"adding alias '{alias}' => '{@using}' to the context"));
                context.Tracer.AddCommand(ReferenceCommand.Using, @using);
                context.Tracer.AddCommand(ReferenceCommand.Alias, alias);
                context.ReferenceAliases[alias] = @using;
            }
            else if (!commands.ContainsKey(ReferenceCommand.Alias) && !commands.ContainsKey(ReferenceCommand.Using))
            {
                // don't actually care, but this makes it clear when the next section of code runs
            }
            else
            {
                _logger.LogError(WithContext(context,
                                             $"either '{nameof(ReferenceCommand.Alias)}' or '{nameof(ReferenceCommand.Using)}' command is set, " +
                                             "but not both - they are only usable together"));
            }
        }

        /// <summary>
        ///     inspect reference and get current Path and Repository
        ///     Repository can be changed by accessing different aliases
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private (string Path, IDictionary<string, string> Repository) HandlePathCommands(CompilationContext context, ReferencePart reference)
        {
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
                    var @using = context.ReferenceAliases[alias];

                    _logger.LogTrace(WithContext(context, $"using alias '{alias}' => '{@using}'"));

                    path = $"{@using}/{rest}";
                }
                else if (alias.Equals("this", StringComparison.OrdinalIgnoreCase))
                {
                    var @using = context.CurrentKey.Substring(0, context.CurrentKey.LastIndexOf('/'));

                    _logger.LogTrace(WithContext(context, $"using special alias '$this' => '{@using}'"));

                    path = $"{@using}/{rest}";
                }
                else if (alias.Equals("struct", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogTrace(WithContext(context, $"using special alias '$struct' => '{rest}', " +
                                                          "switching repository to Structure-Variables"));

                    path = rest;
                    repository = context.StructureInfo.Variables;
                }
                else
                {
                    _logger.LogError($"could not resolve alias '{alias}'");
                }
            }

            return (Path: path, Repository: repository);
        }

        private (string Key, string Value)[] HandleRegionSelection(CompilationContext context,
                                                                   IDictionary<string, string> repository,
                                                                   string path)
        {
            var regionTracer = context.Tracer.AddPathResolution(path);

            var pathMatcher = path.TrimEnd('*');

            _logger.LogTrace(WithContext(context, $"matching '{pathMatcher}' against '{repository.Count}' keys"));

            var matchedItems = repository.Keys
                                         .Where(key => key.StartsWith(pathMatcher, StringComparison.OrdinalIgnoreCase))
                                         .Select(key => (Key: key, Value: repository[key]))
                                         .ToArray();

            _logger.LogTrace(WithContext(context, $"matching '{pathMatcher}' against '{repository.Count}' keys " +
                                                  $"resulted in '{matchedItems.Length}' results"));

            // recursion to resolve keys within the selected region
            var resultItems = new Dictionary<string, string>(matchedItems.Length);
            foreach (var (itemKey, itemValue) in matchedItems)
            {
                var regionContext = new CompilationContext(context)
                {
                    CurrentKey = itemKey,
                    Tracer = regionTracer.AddPathResult(itemKey, itemValue)
                };
                regionContext.IncrementRecursionLevel();

                var regionResult = CompileInternal(regionContext, itemValue);

                foreach (var (k, v) in regionResult)
                {
                    var relativePath = k.Substring(pathMatcher.Length);

                    if (context.CurrentKey.EndsWith('/') || relativePath.StartsWith('/'))
                        resultItems[context.CurrentKey + relativePath] = v;
                    else
                        resultItems[$"{context.CurrentKey}/{relativePath}"] = v;
                }
            }

            // resultItems should be fully resolved
            return resultItems.Select(item => (item.Key, item.Value)).ToArray();
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
                CurrentKey = context.CurrentKey;
                Parser = context.Parser;
            }

            public string CurrentKey { get; set; } = string.Empty;

            public EnvironmentCompilationInfo EnvironmentInfo { get; set; }

            public IConfigurationParser Parser { get; set; }

            public int RecursionLevel { get; private set; }

            public Dictionary<string, string> ReferenceAliases { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public StructureCompilationInfo StructureInfo { get; set; }

            public ITracer Tracer { get; set; }

            public void IncrementRecursionLevel() => RecursionLevel += 1;
        }
    }
}