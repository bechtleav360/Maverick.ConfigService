using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <summary>
    ///     this implementation is meant to be used for one compilation of an Environment + Structure pair, before being discarded
    /// </summary>
    public class DefaultValueResolver : IValueResolver
    {
        private const int KeyRecursionLimit = 100;

        private readonly EnvironmentCompilationInfo _environmentInfo;
        private readonly ILogger _logger;
        private readonly StructureCompilationInfo _structureInfo;
        private readonly IDictionary<ConfigValueProviderType, IConfigValueProvider> _valueProviders;

        public DefaultValueResolver(EnvironmentCompilationInfo environmentInfo,
                                    StructureCompilationInfo structureInfo,
                                    IDictionary<ConfigValueProviderType, IConfigValueProvider> valueProviders,
                                    ILogger logger)
        {
            _environmentInfo = environmentInfo;
            _structureInfo = structureInfo;
            _logger = logger;
            _valueProviders = valueProviders;
        }

        // do an initial check to see if we're dealing with a range-query or not
        // route responsibility to either ResolveValue or ResolveRange
        /// <inheritdoc />
        public Task<IResult<IDictionary<string, string>>> Resolve(string path, string value, ITracer tracer, IConfigurationParser parser)
            => ResolveInternal(new KeyResolveContext(path, value, tracer, parser));

        /// <summary>
        ///     analyze the given parts and determine if they can be compiled and what would be the result
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        private CompilationPlan AnalyzeCompilation(KeyResolveContext context, IList<ConfigValuePart> parts)
        {
            _logger.LogTrace(WithContext(context, "analyzing compilation"));

            // no compilation possible / needed, but still a valid value
            // simplest possible plan - no references and only values
            if (!parts.Any() || parts.All(p => p is ValuePart))
                return new CompilationPlan
                {
                    CompilationPossible = true,
                    CompilationNecessary = false,
                    ContainsRangeQuery = false,
                    Message = string.Empty
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
                    CompilationNecessary = true,
                    ContainsRangeQuery = false,
                    Message = "multiple region-spanning references found"
                };

            // can't reference a region when other stuff would be discarded
            if (regionReferences.Any() && valueParts.Any())
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    CompilationNecessary = true,
                    ContainsRangeQuery = false,
                    Message = "region-spanning reference found within non-reference text"
                };

            // can't compile something that references a region and keys at the same time
            // at least some stuff would be discarded, and that does not qualify as a valid compilation
            if (pathReferences.Length > 1 &&
                pathReferences.Any(p => p.Commands[ReferenceCommand.Path].EndsWith('*')))
                return new CompilationPlan
                {
                    CompilationPossible = false,
                    CompilationNecessary = true,
                    ContainsRangeQuery = false,
                    Message = "region-spanning and value-only references found"
                };

            return new CompilationPlan
            {
                CompilationPossible = true,
                CompilationNecessary = true,
                ContainsRangeQuery = regionReferences.Any(),
                Message = string.Empty
            };
        }

        /// <summary>
        ///     check for recursion-related problems in the current compilation, and return true if errors exist
        /// </summary>
        /// <param name="context"></param>
        /// <param name="errorMessage"></param>
        /// <returns>true if compilation-unit should be aborted due to recursion-problems</returns>
        private bool CheckRecursionErrors(KeyResolveContext context, out string errorMessage)
        {
            if (context.RecursionDepth > KeyRecursionLimit)
            {
                errorMessage = "recursion too deep, aborting compilation";
                context.Tracer.AddError("recursion too deep, aborting compilation");
                return true;
            }

            // only works with at least two items
            // see if Path was already used to compile the value - recursive loop (a - b - c - b)
            // skip the check if no Recursion has happened yet
            if (context.RecursionPath.Count > 1 &&
                context.RecursionPath.First().Path == context.RecursionPath.Last().Path)
            {
                var paths = context.RecursionPath.Select(t => t.Path).ToList();

                var recursionLoopIndex = paths.IndexOf(context.BasePath);
                var beforeLoop = string.Join(" -> ", paths.Take(recursionLoopIndex));
                var loop = string.Join(" => ", paths.Skip(recursionLoopIndex));

                errorMessage = $"recursion-loop detected at {beforeLoop} -> {loop}";
                context.RecursionPath.First().Tracer.AddError(errorMessage);

                return true;
            }

            errorMessage = string.Empty;
            return false;
        }

        private async Task<ReferenceEvaluationResult> EvaluateReference(KeyResolveContext context, ReferencePart reference)
        {
            var evaluationResult = new ReferenceEvaluationResult
            {
                Effects = ReferenceEvaluationType.None,
                ResultingKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            if (reference.Commands.ContainsKey(ReferenceCommand.Alias) && reference.Commands.ContainsKey(ReferenceCommand.Using))
            {
                UpdateContextAliases(context, reference);
                evaluationResult.Effects |= ReferenceEvaluationType.ModifiedContext;
            }

            if (reference.Commands.ContainsKey(ReferenceCommand.Path))
            {
                var (resolveType, resolvedEntries) = await ResolveReferencePath(context, reference);

                foreach (var (k, v) in resolvedEntries)
                    evaluationResult.ResultingKeys[k] = v;

                evaluationResult.Effects |= resolveType;
            }

            return evaluationResult;
        }

        private async Task<IResult<IDictionary<string, string>>> ResolveInternal(KeyResolveContext context)
        {
            if (CheckRecursionErrors(context, out var errorMessage))
            {
                _logger.LogWarning(WithContext(context, errorMessage));
                return Result.Success<IDictionary<string, string>>(new Dictionary<string, string> {{context.BasePath, string.Empty}});
            }

            if (context.OriginalValue is null)
                return Result.Success<IDictionary<string, string>>(new Dictionary<string, string> {{context.BasePath, null}});

            var parts = context.Parser.Parse(context.OriginalValue);

            var plan = AnalyzeCompilation(context, parts);

            // no changes necessary
            if (!plan.CompilationNecessary)
                return Result.Success<IDictionary<string, string>>(new Dictionary<string, string> {{context.BasePath, context.OriginalValue}});

            if (!plan.CompilationPossible)
                return Result.Error<IDictionary<string, string>>($"can't compile key: '{plan.Message}'", ErrorCode.InvalidData);

            IDictionary<string, string> results;

            if (!plan.ContainsRangeQuery)
            {
                var valueResult = await ResolveValue(context, parts);

                // bubble error up without special handling
                if (valueResult.IsError)
                    return valueResult;

                results = valueResult.Data;
            }
            else
            {
                _logger.LogTrace(WithContext(context, $"found range-query in '{context.BasePath}', resolving as query"));
                var rangeResult = await ResolveRange(context, parts);

                if (rangeResult.IsError)
                    return rangeResult;

                results = rangeResult.Data;
            }

            return Result.Success(results);
        }

        private string ResolvePathAliases(KeyResolveContext context, string path)
        {
            var currentResolvedPath = path;

            var aliasResolved = false;
            do
            {
                // @TODO: add hard-coded $this alias that resolves to ($path/$value => $path)
                // replace all $alias with their collected replacements
                // do until we don't find any more replacements
                foreach (var (aliasName, @using) in context.Aliases)
                {
                    var alias = $"${aliasName}";

                    if (!currentResolvedPath.Contains(alias))
                        continue;

                    currentResolvedPath = currentResolvedPath.Replace(alias, @using);
                    aliasResolved = true;
                }
            } while (aliasResolved);

            return currentResolvedPath;
        }

        /// <summary>
        ///     implementation of <see cref="IValueResolver.Resolve" /> for range-queries
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        private async Task<IResult<IDictionary<string, string>>> ResolveRange(KeyResolveContext context, IList<ConfigValuePart> parts)
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in parts)
                if (part is ValuePart v)
                {
                    context.Tracer.AddError($"found static value '{v.Text}', dismissing due to previously found range-query");
                    _logger.LogInformation(WithContext(context, $"found static value '{v.Text}', dismissing due to previously found range-query"));
                }
                else if (part is ReferencePart reference)
                {
                    var evaluationResult = await EvaluateReference(context, reference);

                    if (evaluationResult.Effects.HasFlag(ReferenceEvaluationType.ResolvedDirectReference))
                    {
                        var path = reference.Commands[ReferenceCommand.Path];
                        context.Tracer.AddError($"ref '{path}'; expected range-query, got direct-ref, discarding invalid value");
                        _logger.LogWarning(WithContext(context, $"ref '{path}'; expected range-query, got direct-ref, discarding invalid value"));
                        continue;
                    }

                    if (evaluationResult.Effects.HasFlag(ReferenceEvaluationType.ResolvedRangeQuery))
                        foreach (var (key, value) in evaluationResult.ResultingKeys)
                            results[key] = value;
                }
                else
                {
                    return Result.Error<IDictionary<string, string>>($"unknown part parsed: '{part.GetType().Name}'", ErrorCode.InvalidData);
                }

            return Result.Success<IDictionary<string, string>>(results);
        }

        private async Task<(ReferenceEvaluationType, Dictionary<string, string>)> ResolveReferencePath(KeyResolveContext context, ReferencePart reference)
        {
            // this is to check for possible fallback-values, if actual path-resolution goes wrong
            bool FallbackAction()
            {
                if (!reference.Commands.ContainsKey(ReferenceCommand.Fallback)) return false;

                var fallbackValue = reference.Commands[ReferenceCommand.Fallback];
                context.Tracer.AddWarning($"using fallback '{fallbackValue}' after failing to resolve '{context.BasePath}'");
                _logger.LogInformation($"using fallback '{fallbackValue}' after failing to resolve '{context.BasePath}'");
                return true;
            }

            // this is basically a structuring for later
            // - use some action to get values
            // - check if operation succeeded
            // - if it failed, execute given fallback
            async Task ResolveValueInternal<T>(Task<IResult<T>> resolveValueTask, Action<T> successAction, Func<bool> fallbackAction)
            {
                var result = await resolveValueTask;
                if (!result.IsError)
                    successAction?.Invoke(result.Data);

                context.Tracer.AddError($"could not resolve path '{context.BasePath}' ({result.Message})");
                _logger.LogWarning(WithContext(context, $"could not resolve values: ({result.Code:G}) {result.Message}"));

                fallbackAction?.Invoke();
            }

            var resultType = ReferenceEvaluationType.None;
            var intermediateResult = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var actualResult = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var referencePath = ResolvePathAliases(context, reference.Commands[ReferenceCommand.Path]);
            var (provider, newPath) = SelectConfigValueProvider(context, referencePath);
            if (provider is null)
            {
                _logger.LogWarning(WithContext(context, "could not resolve any ValueProvider"));
                return (resultType, intermediateResult);
            }

            // removes $stuff from the beginning of referencePath, if a suitable provider could be found
            referencePath = newPath;

            var rangeTracer = context.Tracer.AddPathResolution(referencePath);

            if (referencePath.EndsWith('*'))
            {
                await ResolveValueInternal(provider.TryGetRange(referencePath),
                                           data =>
                                           {
                                               foreach (var (key, value) in data)
                                               {
                                                   var compositePath = $"{context.BasePath}/{key.TrimStart('/')}";
                                                   intermediateResult[compositePath] = value;
                                                   rangeTracer.AddPathResult(compositePath, value);
                                               }
                                           },
                                           FallbackAction);

                resultType = ReferenceEvaluationType.ResolvedRangeQuery;
            }
            else
            {
                await ResolveValueInternal(provider.TryGetValue(referencePath),
                                           data =>
                                           {
                                               intermediateResult[context.BasePath] = data;
                                               rangeTracer.AddPathResult(data);
                                           },
                                           FallbackAction);

                resultType = ReferenceEvaluationType.ResolvedDirectReference;
            }

            foreach (var (nextKey, nextValue) in intermediateResult)
            {
                var nextTracer = context.Tracer.AddPathResolution(nextValue);
                var nextContext = context.CreateChildContext(nextKey, nextValue, nextTracer);
                var nextResult = await ResolveInternal(nextContext);

                // @TODO: should we maybe do something else here instead of continuing on like nothing happened?
                if (nextResult.IsError)
                    continue;

                foreach (var entry in nextResult.Data)
                    actualResult[entry.Key] = entry.Value;
            }

            return (resultType, actualResult);
        }

        /// <summary>
        ///     implementation of <see cref="IValueResolver.Resolve" /> for direct references
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        private async Task<IResult<IDictionary<string, string>>> ResolveValue(KeyResolveContext context, IList<ConfigValuePart> parts)
        {
            var builder = new StringBuilder();

            foreach (var part in parts)
                switch (part)
                {
                    case ValuePart v:
                        _logger.LogTrace(WithContext(context, $"appending static value '{v.Text}'"));
                        context.Tracer.AddStaticValue(v.Text);
                        builder.Append(v.Text);
                        continue;

                    case ReferencePart reference:
                        var evaluationResult = await EvaluateReference(context, reference);
                        if (evaluationResult.Effects.HasFlag(ReferenceEvaluationType.ResolvedRangeQuery))
                        {
                            context.Tracer.AddError("key has been extended using range-query, discarding invalid results");
                            _logger.LogWarning(WithContext(context, "key has been extended using range-query, discarding invalid results"));
                            continue;
                        }

                        if (evaluationResult.Effects.HasFlag(ReferenceEvaluationType.ResolvedDirectReference))
                        {
                            // will be empty if value could not be found, or provider has other issues
                            var value = evaluationResult.ResultingKeys.FirstOrDefault().Value;
                            builder.Append(value ?? string.Empty);
                        }

                        break;

                    default:
                        return Result.Error<IDictionary<string, string>>($"unknown part parsed: '{part.GetType().Name}'", ErrorCode.InvalidData);
                }

            return Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
            {
                {context.BasePath, builder.ToString()}
            });
        }

        private (IConfigValueProvider, string) SelectConfigValueProvider(KeyResolveContext context, string path)
        {
            // will be set once a valid provider is found
            IConfigValueProvider provider = null;
            var modifiedPath = path;

            var providerTypeAssociations = new Dictionary<string, ConfigValueProviderType>
            {
                {"$secret", ConfigValueProviderType.SecretStore},
                {"$struct", ConfigValueProviderType.StructVariables}
            };

            var fallbackProviderType = ConfigValueProviderType.Environment;

            // try to get the correct provider by inspecting the start of the path
            // $secret should be evaluated to the registered SecretStore by its type
            foreach (var (typeHandle, type) in providerTypeAssociations)
                // if we need a specific provider but it isn't registered for this key => that's a problem
                if (path.StartsWith(typeHandle, StringComparison.OrdinalIgnoreCase))
                    if (_valueProviders.TryGetValue(type, out provider))
                    {
                        // remove $stuff/ from the beginning of path
                        modifiedPath = path.Substring(typeHandle.Length)
                                           .TrimStart('/');
                    }
                    else
                    {
                        _logger.LogWarning(WithContext(context, $"no provider registered for type '{type}'"));
                        return (null, null);
                    }

            // if no fallbackprovider can be resolved it's another - more serious - problem
            if (provider is null)
                if (!_valueProviders.TryGetValue(fallbackProviderType, out provider))
                {
                    _logger.LogWarning(WithContext(context, "no default-provider found"));
                    return (null, null);
                }

            return (provider, modifiedPath);
        }

        private void UpdateContextAliases(KeyResolveContext context, ReferencePart reference)
        {
            var alias = reference.Commands[ReferenceCommand.Alias];
            var @using = reference.Commands[ReferenceCommand.Using];

            _logger.LogTrace(WithContext(context, $"adding alias '{alias}'='{@using}'"));

            context.Tracer.AddCommand(ReferenceCommand.Alias, alias);
            context.Tracer.AddCommand(ReferenceCommand.Using, @using);

            context.Aliases[alias] = @using;
        }

        private string WithContext(KeyResolveContext context, string message)
            => $"'{_environmentInfo.Name}' / '{_structureInfo.Name}' / '{context.BasePath}' {message}";

        private struct CompilationPlan
        {
            /// <summary>
            ///     compilation is possible to be executed - no invalid references
            /// </summary>
            public bool CompilationPossible { get; set; }

            /// <summary>
            ///     compilation is necessary due to parsed References
            /// </summary>
            public bool CompilationNecessary { get; set; }

            /// <summary>
            ///     flag indicating that this key will likely be expanded to N keys
            /// </summary>
            public bool ContainsRangeQuery { get; set; }

            /// <summary>
            ///     error-message in case either flag indicates errors
            /// </summary>
            public string Message { get; set; }
        }

        private struct ReferenceEvaluationResult
        {
            /// <inheritdoc cref="ReferenceEvaluationType" />
            public ReferenceEvaluationType Effects { get; set; }

            /// <summary>
            ///     collection of keys/values that were returned while evaluating the given Reference
            /// </summary>
            public Dictionary<string, string> ResultingKeys { get; set; }
        }

        /// <summary>
        ///     flags indicating what has happened while evaluating a given Reference
        /// </summary>
        [Flags]
        private enum ReferenceEvaluationType
        {
            /// <summary>
            ///     nothing has happened while evaluating a given Reference
            /// </summary>
            None = 0,

            /// <summary>
            ///     the current context has been modified while evaluating the Reference
            /// </summary>
            ModifiedContext = 1,

            /// <summary>
            ///     a direct reference to another key has been evaluated
            /// </summary>
            ResolvedDirectReference = 1 << 1,

            /// <summary>
            ///     a query for a range of keys has been evaluated
            /// </summary>
            ResolvedRangeQuery = 1 << 2
        }

        private class KeyResolveContext
        {
            // only kept between different references in same key - thus not kept between KeyContext instances
            public readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public readonly string BasePath;

            public readonly string OriginalValue;

            public readonly IConfigurationParser Parser;

            public readonly int RecursionDepth;

            public readonly List<(string Path, ITracer Tracer)> RecursionPath;

            public readonly ITracer Tracer;

            public KeyResolveContext(string basePath, string originalValue, ITracer tracer, IConfigurationParser parser)
                : this(basePath, originalValue, tracer, parser, 0, new (string, ITracer)[0])
            {
            }

            private KeyResolveContext(string basePath,
                                      string originalValue,
                                      ITracer tracer,
                                      IConfigurationParser parser,
                                      int recursionDepth,
                                      IEnumerable<(string, ITracer)> recursionPath)
            {
                BasePath = basePath;
                OriginalValue = originalValue;
                Tracer = tracer;
                Parser = parser;
                RecursionDepth = recursionDepth;
                RecursionPath = recursionPath.ToList();
            }

            /// <summary>
            ///     create a derived context, keeping some existing values and overriding some others
            /// </summary>
            /// <param name="nextPath"></param>
            /// <param name="nextValue"></param>
            /// <param name="nextTracer"></param>
            /// <returns></returns>
            public KeyResolveContext CreateChildContext(string nextPath, string nextValue, ITracer nextTracer)
                => new KeyResolveContext(nextPath,
                                         nextValue,
                                         nextTracer,
                                         Parser,
                                         RecursionDepth + 1,
                                         RecursionPath.Append((nextValue, Tracer)));
        }
    }
}