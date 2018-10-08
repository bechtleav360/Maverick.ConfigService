using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.Compilation
{
    public class ConfigurationCompiler : IConfigurationCompiler
    {
        private readonly ILogger<ConfigurationCompiler> _logger;

        public ConfigurationCompiler(ILogger<ConfigurationCompiler> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> Compile(IDictionary<string, string> repository,
                                                               IDictionary<string, string> referencer,
                                                               IConfigurationParser parser,
                                                               CompilationOptions options)
        {
            IDictionary<string, string> compiledConfiguration = new Dictionary<string, string>();

            var stack = new Stack<KeyValuePair<string, string>>(referencer);

            // count how many times a given key was processed
            // processing a key is stopped once it reaches the defined recursion threshold
            var processingCounter = new Dictionary<string, int>();

            while (stack.TryPop(out var kvp))
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // add the key to the counter if necessary, and increase it
                if (!processingCounter.ContainsKey(key))
                    processingCounter[key] = 0;
                processingCounter[key] += 1;

                if (processingCounter[key] > options.RecursionLimit)
                {
                    _logger.LogError($"key '{key}' has reached the threshold for recursion and will not be processed further");
                    compiledConfiguration[key] = value;
                    continue;
                }

                var processedValues = await ResolveReferences(repository, referencer, key, value, parser, options);

                switch (processedValues.Count)
                {
                    case var n when n <= 0:
                        _logger.LogWarning($"could not compile '{key}' => '{value}', see previous messages for more information");
                        break;

                    case 1:
                    {
                        var processedValue = processedValues.First().Value;
                        _logger.LogTrace($"compiled '{key}' => '{processedValue}'");

                        if (processedValue.Equals(value, StringComparison.OrdinalIgnoreCase))
                            compiledConfiguration[key] = value;
                        // push back to stack for one more pass
                        else
                            stack.Push(new KeyValuePair<string, string>(key, processedValue));
                        break;
                    }

                    case var n when n > 1:
                    {
                        foreach (var processedValue in processedValues)
                        {
                            _logger.LogTrace($"compiled '{key}' => {processedValue.Key} => '{processedValue.Value}'");
                            // push back to stack for one more pass
                            stack.Push(processedValue);
                        }

                        break;
                    }
                }
            }

            return compiledConfiguration;
        }

        private async Task<IDictionary<string, string>> ResolveReferences(IDictionary<string, string> repository,
                                                                          IDictionary<string, string> referencer,
                                                                          string key,
                                                                          string value,
                                                                          IConfigurationParser parser,
                                                                          CompilationOptions options)
        {
            var context = new ReferenceContext();
            var parseResult = parser.Parse(value);

            // if there are no references to be found, just return the given value without further ado
            if (!parseResult.OfType<ReferencePart>().Any())
            {
                _logger.LogDebug($"no references found in '{value}', nothing to resolve");
                return new Dictionary<string, string> {{key, value}};
            }

            // if we're not allowed to resolve references anyway, return without processing
            if (!options.References.HasFlag(ReferenceOption.AllowRepositoryReference) && !options.References.HasFlag(ReferenceOption.AllowSelfReference))
            {
                _logger.LogWarning($"'{parseResult.OfType<ReferencePart>().Count()}' references found, but options forbid resolving " +
                                   $"(requires {ReferenceOption.AllowRepositoryReference:G} | {ReferenceOption.AllowSelfReference:G})");

                return new Dictionary<string, string> {{key, value}};
            }

            // initialize StringBuilder with starting size of the input,
            // this may reduce additional increases in size while assembling the parts again
            var valueBuilder = new StringBuilder(value.Length);

            foreach (var part in parseResult)
            {
                switch (part)
                {
                    case ReferencePart referencePart:
                        var resolvedReference = await ResolveReference(repository, referencer, context, referencePart, options);

                        // if the reference only modifies the context we don't have to append anything to the result
                        if (!resolvedReference.HasValue)
                            continue;

                        // if the reference resolves to a simple string we append it to valueBuilder
                        if (resolvedReference.IsSimple)
                            valueBuilder.Append(resolvedReference.SimpleValue);

                        // if the reference resolves to multiple keys (is complex) we return it
                        // and ignore ValueParts before / after, and don't further process this values
                        else if (resolvedReference.IsComplex)
                            return resolvedReference.ExpandedValue
                                                    .ToDictionary(kvp => $"{key}/{kvp.Key}".Replace("//", ""),
                                                                  kvp => kvp.Value);

                        // otherwise it's probably a new kind of result that someone forgot to handle here
                        else
                            _logger.LogError($"unknown result received from {nameof(ResolveReference)}");

                        break;

                    case ValuePart valuePart:
                        // ValueParts don't have any special meaning and are added back without further processing
                        valueBuilder.Append(valuePart.Text);
                        break;

                    default:
                        _logger.LogCritical($"handling of '{part.GetType().Name}' is not implemented");
                        continue;
                }
            }

            return new Dictionary<string, string> {{key, valueBuilder.ToString()}};
        }

        /// <summary>
        ///     resolve <see cref="ReferenceCommand.Alias"/> and <see cref="ReferenceCommand.Using"/> if possible.
        ///     this will modify the given context if successful.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private async Task ResolveAliasCommand(ReferenceContext context, ReferencePart reference)
        {
            await Task.Yield();

            var usingCommand = reference.Commands.ContainsKey(ReferenceCommand.Using)
                                   ? reference.Commands[ReferenceCommand.Using]
                                   : null;

            var aliasCommand = reference.Commands.ContainsKey(ReferenceCommand.Alias)
                                   ? reference.Commands[ReferenceCommand.Alias]
                                   : null;

            // if one or the other, but not both are set
            // log an invalid command, because they have to appear in tandem
            if (usingCommand is null && !(aliasCommand is null) ||
                !(usingCommand is null) && aliasCommand is null)
            {
                _logger.LogError($"commands {ReferenceCommand.Using:G} and {ReferenceCommand.Alias:G} " +
                                 "have to appear in tandem, they can't appear alone");
            }
            // if neither are null
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // go home ReSharper, you're drunk. I tested this manually and it works as intended
            else if (!(usingCommand is null) && !(aliasCommand is null))
            {
                // add the alias to the context
                _logger.LogTrace($"adding alias '{aliasCommand}' => '{usingCommand}' to the context");
                context.Aliases[aliasCommand] = usingCommand;
            }

            // else {} => both are null, but we don't care
        }

        private async Task<ReferenceResolveResult> ResolveReference(IDictionary<string, string> repository,
                                                                    IDictionary<string, string> referencer,
                                                                    ReferenceContext context,
                                                                    ReferencePart reference,
                                                                    CompilationOptions options)
        {
            await ResolveAliasCommand(context, reference);

            var pathCommand = reference.Commands.ContainsKey(ReferenceCommand.Path)
                                  ? reference.Commands[ReferenceCommand.Path]
                                  : null;

            if (pathCommand is null)
                return new ReferenceResolveResult();

            // if the path starts with a "$", it's using an alias
            if (pathCommand.StartsWith("$"))
            {
                var split = pathCommand.Split('/', 2);
                var alias = split[0].TrimStart('$');
                var rest = split[1];

                if (context.Aliases.ContainsKey(alias))
                {
                    _logger.LogDebug($"de-referencing alias '{alias}'");
                    pathCommand = $"{context.Aliases[alias]}/{rest}".Replace("//", "/");
                }
                else
                {
                    _logger.LogWarning($"could not de-reference alias '{alias}'");
                }
            }

            // Path points indicates we match against a number of keys, so we return a 'complex' result
            if (pathCommand.EndsWith('*'))
            {
                var matcher = pathCommand.TrimEnd('*');

                // if the required flag is set, search the given bucket and return stuff if possible
                // return with first results
                foreach (var (dataBucket, requiredFlag) in new[]
                {
                    (referencer, ReferenceOption.AllowSelfReference),
                    (repository, ReferenceOption.AllowRepositoryReference)
                })
                {
                    if (!options.References.HasFlag(requiredFlag))
                        continue;

                    var result = dataBucket.Where(kvp => kvp.Key.StartsWith(matcher, StringComparison.OrdinalIgnoreCase))
                                           .ToDictionary(kvp => kvp.Key.Substring(matcher.Length),
                                                         kvp => kvp.Value);

                    if (result.Any())
                        return new ReferenceResolveResult(result);
                }

                _logger.LogError("either no references allowed or no data could be found");

                // return empty string, reference could not be resolved properly
                return new ReferenceResolveResult(string.Empty);
            }

            // if the required flag is set, search the given bucket and return stuff if possible
            // return with first results
            foreach (var (dataBucket, requiredFlag) in new[]
            {
                (referencer, ReferenceOption.AllowSelfReference),
                (repository, ReferenceOption.AllowRepositoryReference)
            })
            {
                if (!options.References.HasFlag(requiredFlag))
                    continue;

                var result = dataBucket.Where(kvp => kvp.Key.StartsWith(pathCommand, StringComparison.OrdinalIgnoreCase))
                                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (result.Count == 0)
                    return new ReferenceResolveResult(string.Empty);
                if (result.Count == 1)
                    return new ReferenceResolveResult(result.First().Value);
                if (result.Count > 1)
                {
                    // should this be an error instead? ¯\_(ツ)_/¯
                    _logger.LogError($"reference does not contain a '*' to indicate a complex match, but matched '{result.Keys.Count}' keys");
                    return new ReferenceResolveResult();
                }
            }

            _logger.LogError("no keys matched the path or reference-resolving is not allowed");
            return new ReferenceResolveResult(string.Empty);
        }

        private class ReferenceContext
        {
            public Dictionary<string, string> Aliases { get; } = new Dictionary<string, string>();
        }

        // ReSharper disable once CommentTypo - let me make my joke, ReSharper
        // ARRR
        private class ReferenceResolveResult
        {
            public string SimpleValue { get; }

            public IReadOnlyDictionary<string, string> ExpandedValue { get; }

            public bool HasValue => IsSimple || IsComplex;

            public bool IsSimple => SimpleValue != null;

            public bool IsComplex => ExpandedValue != null;

            /// <summary>
            ///     create a result that does not carry any values, and only modifies the context
            /// </summary>
            public ReferenceResolveResult()
            {
            }

            /// <summary>
            ///     create a result that carries a simple value with it
            /// </summary>
            /// <param name="value"></param>
            public ReferenceResolveResult(string value)
            {
                SimpleValue = value ?? throw new ArgumentNullException(nameof(value));
            }

            /// <summary>
            ///     create a result that carries a complex object-structure with it
            /// </summary>
            /// <param name="expandedValue"></param>
            public ReferenceResolveResult(IDictionary<string, string> expandedValue)
            {
                ExpandedValue = new ReadOnlyDictionary<string, string>(expandedValue ?? throw new ArgumentNullException(nameof(expandedValue)));
            }
        }
    }
}