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
        public async Task<IDictionary<string, string>> Compile(IDictionary<string, string> environment,
                                                               IDictionary<string, string> structure,
                                                               IConfigurationParser parser)
        {
            IDictionary<string, string> compiledConfiguration = new Dictionary<string, string>();

            var stack = new Stack<KeyValuePair<string, string>>(structure);

            while (stack.TryPop(out var kvp))
            {
                var key = kvp.Key;
                var value = kvp.Value;

                _logger.LogDebug($"compiling '{key}' => '{value}'");

                var processedValues = await ResolveReferences(environment, key, value, parser);

                if (processedValues.Count == 0)
                {
                    _logger.LogWarning($"could not compile '{key}' => '{value}', see previous messages for more information");
                }
                else if (processedValues.Count == 1)
                {
                    var processedValue = processedValues.First().Value;
                    _logger.LogTrace($"compiled '{key}' => '{processedValue}'");

                    if (processedValue.Equals(value, StringComparison.OrdinalIgnoreCase))
                        compiledConfiguration[key] = value;
                    // push back to stack for one more pass
                    else
                        stack.Push(new KeyValuePair<string, string>(key, processedValue));
                }
                else if (processedValues.Count > 1)
                {
                    foreach (var processedValue in processedValues)
                    {
                        _logger.LogTrace($"compiled '{key}' => {processedValue.Key} => '{processedValue.Value}'");
                        // push back to stack for one more pass
                        stack.Push(processedValue);
                    }
                }
            }

            return compiledConfiguration;
        }

        private async Task<IDictionary<string, string>> ResolveReferences(IDictionary<string, string> environment,
                                                                          string key,
                                                                          string value,
                                                                          IConfigurationParser parser)
        {
            var context = new ReferenceContext();
            var parseResult = parser.Parse(value);

            // if there are no references to be found, just return the given value without further ado
            if (!parseResult.OfType<ReferencePart>().Any())
            {
                _logger.LogDebug($"no references found in '{value}', nothing to resolve");
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
                        var resolvedReference = await ResolveReference(environment, context, referencePart);

                        // if the reference only modifies the context, we don't have to append anything to the result
                        if (!resolvedReference.HasValue)
                            continue;

                        if (resolvedReference.IsSimple)
                            valueBuilder.Append(resolvedReference.SimpleValue);

                        else if (resolvedReference.IsComplex)
                            return resolvedReference.ExpandedValue
                                                    .ToDictionary(kvp => $"{key}/{kvp.Key}".Replace("//", ""),
                                                                  kvp => kvp.Value);

                        else
                            _logger.LogError($"unknown result received from {nameof(ResolveReference)}");

                        break;

                    case ValuePart valuePart:
                        valueBuilder.Append(valuePart.Text);
                        break;

                    default:
                        _logger.LogCritical($"handling of '{part.GetType().Name}' is not implemented");
                        return new Dictionary<string, string> {{key, value}};
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

        private async Task<ReferenceResolveResult> ResolveReference(IDictionary<string, string> environment,
                                                                    ReferenceContext context,
                                                                    ReferencePart reference)
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

                var matchingData = environment.Where(kvp => kvp.Key.StartsWith(matcher, StringComparison.OrdinalIgnoreCase))
                                              .ToDictionary(kvp => kvp.Key.Substring(matcher.Length),
                                                            kvp => kvp.Value);

                return new ReferenceResolveResult(matchingData);
            }

            var result = environment.Where(kvp => kvp.Key.StartsWith(pathCommand, StringComparison.OrdinalIgnoreCase))
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // if we have only one hit, return a 'simple' resolved value
            if (result.Keys.Count == 1)
                return new ReferenceResolveResult(result[result.Keys.First()]);

            // should this be an error instead? ¯\_(ツ)_/¯
            _logger.LogError($"reference does not contain a '*' to indicate a complex match, but matched '{result.Keys.Count}' keys");
            return new ReferenceResolveResult();
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