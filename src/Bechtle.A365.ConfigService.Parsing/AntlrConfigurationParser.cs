using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bechtle.A365.ConfigService.Parsing
{
    /// <inheritdoc cref="IConfigurationParser" />
    public class AntlrConfigurationParser : ConfigReferenceParserBaseVisitor<ConfigValuePart[]>, IConfigurationParser
    {
        private readonly Dictionary<string, ReferenceCommand> _commandLookup =
            new Dictionary<string, ReferenceCommand>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"using", ReferenceCommand.Using},
                {"alias", ReferenceCommand.Alias},
                {"path", ReferenceCommand.Path},
                {"default", ReferenceCommand.Fallback},
                {"fallback", ReferenceCommand.Fallback}
            };

        private readonly ILogger<AntlrConfigurationParser> _logger;

        public AntlrConfigurationParser(ILogger<AntlrConfigurationParser> logger = null)
        {
            _logger = logger ?? new NullLogger<AntlrConfigurationParser>();
        }

        /// <inheritdoc />
        public List<ConfigValuePart> Parse(string text)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace($"parsing: {text}");

            var errorListener = new AntlrErrorListener(_logger);

            var inputStream = new AntlrInputStream(text);

            var lexer = new ConfigReferenceLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            var commonTokenStream = new CommonTokenStream(lexer);

            var parser = new ConfigReferenceParser(commonTokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            parser.Context = parser.input();

            var result = Visit(parser.Context)?.ToList() ?? new List<ConfigValuePart>();

            return result;
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitChildren(IRuleNode node)
            => Enumerable.Range(0, node.ChildCount)
                         .Select(node.GetChild)
                         .Select(Visit)
                         .Where(_ => !(_ is null) && _.Any())
                         .SelectMany(_ => _)
                         .ToArray();

        /// <inheritdoc />
        public override ConfigValuePart[] VisitCommandRecursive(ConfigReferenceParser.CommandRecursiveContext context)
            => VisitReferenceInternal(context,
                                      c => c.REF_CMND_NAME().GetText(),
                                      c => string.Empty,
                                      true);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitCommandReference(ConfigReferenceParser.CommandReferenceContext context)
            => VisitReferenceInternal(context,
                                      c => c.REF_CMND_NAME().GetText(),
                                      c => string.Empty,
                                      false);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFluff(ConfigReferenceParser.FluffContext context)
        {
            try
            {
                var builder = new StringBuilder();
                foreach (var fluff in context.FLUFF().Concat(context.SINGLE_BRACES())
                                             .OrderBy(t => t.Symbol.StartIndex))
                    builder.Append(fluff.Symbol.Text);

                var result = builder.ToString();

                if (_logger.IsEnabled(LogLevel.Trace))
                    _logger.LogTrace($"adding ValuePart: {result}");

                return new ConfigValuePart[] {new ValuePart(result)};
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to add ValuePart: {e}");
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFullRecursive(ConfigReferenceParser.FullRecursiveContext context)
            => VisitReferenceInternal(context,
                                      c => c.REF_CMND_NAME().GetText(),
                                      c => SanitizeReferenceValue(c.REF_CMND_VAL().GetText()),
                                      true);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFullReference(ConfigReferenceParser.FullReferenceContext context)
            => VisitReferenceInternal(context,
                                      c => c.REF_CMND_NAME().GetText(),
                                      c => SanitizeReferenceValue(c.REF_CMND_VAL().GetText()),
                                      false);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitInput(ConfigReferenceParser.InputContext context) => VisitChildren(context);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitReference(ConfigReferenceParser.ReferenceContext context)
        {
            try
            {
                var children = VisitChildren(context);

                var commands = children.OfType<ReferencePart>()
                                       .SelectMany(r => r.Commands.ToArray())
                                       .ToArray();

                _logger.LogTrace($"ReferencePart is being assembled from '{commands.Length} / {children.Length}' commands");

                // collect the commands of all sub-references into one result-dictionary
                return new ConfigValuePart[]
                {
                    new ReferencePart(commands.ToDictionary(c => c.Key, c => c.Value))
                };
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to parse reference: {e}");
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitValueRecursive(ConfigReferenceParser.ValueRecursiveContext context)
            => VisitReferenceInternal(context,
                                      ReferenceCommand.Path,
                                      c => SanitizeReferenceValue(c.REF_CMND_VAL().GetText()),
                                      true);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitValueReference(ConfigReferenceParser.ValueReferenceContext context)
            => VisitReferenceInternal(context,
                                      ReferenceCommand.Path,
                                      c => SanitizeReferenceValue(c.REF_CMND_VAL().GetText()),
                                      false);

        private ReferenceCommand ParseCommand(string keyword)
        {
            if (_commandLookup.ContainsKey(keyword))
                return _commandLookup[keyword];

            throw new Exception($"keyword '{keyword}' not supported");
        }

        private string SanitizeReferenceCommand(string command) => command.Trim()
                                                                          .TrimEnd(':')
                                                                          .TrimEnd();

        private string SanitizeReferenceValue(string value) =>
            TrimWhitespace(
                TrimSurroundingQuotes(
                    TrimTrailingSemicolon(value)));

        private string TrimSurroundingQuotes(string value) => value.StartsWith("\"") &&
                                                              value.EndsWith("\"")
                                                                  ? value.TrimStart('"')
                                                                         .TrimEnd('"')
                                                                  : value;

        private string TrimTrailingSemicolon(string value) => value.TrimEnd(';');

        private string TrimWhitespace(string value) => value.Trim();

        private ConfigValuePart[] VisitReferenceInternal<T>(T context,
                                                            ReferenceCommand command,
                                                            Func<T, string> valueSelector,
                                                            bool usingChildren)
            where T : IRuleNode
        {
            try
            {
                return VisitReferenceInternal(context, command, valueSelector(context), usingChildren);
            }
            catch (Exception e)
            {
                _logger.LogError($"could not parse to ReferencePart: {e}");
                return new ConfigValuePart[0];
            }
        }

        private ConfigValuePart[] VisitReferenceInternal<T>(T context,
                                                            Func<T, string> nameSelector,
                                                            Func<T, string> valueSelector,
                                                            bool usingChildren)
            where T : IRuleNode
        {
            try
            {
                return VisitReferenceInternal(context,
                                              ParseCommand(SanitizeReferenceCommand(nameSelector(context))),
                                              valueSelector(context),
                                              usingChildren);
            }
            catch (Exception e)
            {
                _logger.LogError($"could not parse to ReferencePart: {e}");
                return new ConfigValuePart[0];
            }
        }

        private ConfigValuePart[] VisitReferenceInternal(IRuleNode context, ReferenceCommand command, string value, bool usingChildren)
        {
            try
            {
                var commands = new Dictionary<ReferenceCommand, string> {{command, value}};

                if (usingChildren)
                    return new ConfigValuePart[] {new ReferencePart(commands)}.Concat(VisitChildren(context))
                                                                              .ToArray();

                return new ConfigValuePart[] {new ReferencePart(commands)};
            }
            catch (Exception e)
            {
                _logger.LogError($"could not parse to ReferencePart: {e}");
                return new ConfigValuePart[0];
            }
        }
    }
}