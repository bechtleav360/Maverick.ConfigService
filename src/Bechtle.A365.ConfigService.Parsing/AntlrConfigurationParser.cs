using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Bechtle.A365.ConfigService.Parsing
{
    /// <inheritdoc cref="IConfigurationParser" />
    public class AntlrConfigurationParser : ConfigReferenceParserBaseVisitor<ConfigValuePart[]>, IConfigurationParser
    {
        /// <inheritdoc />
        public List<ConfigValuePart> Parse(string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new ConfigReferenceLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new ConfigReferenceParser(commonTokenStream);

            parser.Context = parser.input();

            return Visit(parser.Context)?.ToList() ?? new List<ConfigValuePart>();
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitInput(ConfigReferenceParser.InputContext context) => VisitChildren(context);

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFluff(ConfigReferenceParser.FluffContext context)
        {
            var builder = new StringBuilder();
            foreach (var fluff in context.FLUFF().Concat(context.SINGLE_BRACES())
                                         .OrderBy(t => t.Symbol.StartIndex))
            {
                builder.Append(fluff.Symbol.Text);
            }

            return new ConfigValuePart[] {new ValuePart(builder.ToString())};
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
        public override ConfigValuePart[] VisitReference(ConfigReferenceParser.ReferenceContext context)
        {
            var children = VisitChildren(context);

            // collect the commands of all sub-references into one result-dictionary
            return new ConfigValuePart[]
            {
                new ReferencePart(children.OfType<ReferencePart>()
                                          .SelectMany(r => r.Commands.ToArray())
                                          .ToDictionary(c => c.Key, c => c.Value))
            };
        }

        /// <inheritdoc />
        public override ConfigValuePart[] Visit(IParseTree tree)
        {
            var result = base.Visit(tree);
            return result;
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitCommandReference(ConfigReferenceParser.CommandReferenceContext context)
        {
            try
            {
                var commandName = context.REF_CMND_NAME().GetText();

                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {ParseCommand(SanitizeReferenceCommand(commandName)), string.Empty}
                };

                return new ConfigValuePart[] {new ReferencePart(commands)};
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitCommandRecursive(ConfigReferenceParser.CommandRecursiveContext context)
        {
            try
            {
                var commandName = context.REF_CMND_NAME().GetText();

                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {ParseCommand(SanitizeReferenceCommand(commandName)), string.Empty}
                };

                return new ConfigValuePart[] {new ReferencePart(commands)}.Concat(VisitChildren(context))
                                                                          .ToArray();
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitValueReference(ConfigReferenceParser.ValueReferenceContext context)
        {
            try
            {
                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {
                        ReferenceCommand.Path,
                        SanitizeReferenceValue(context.REF_CMND_VAL()
                                                      .GetText())
                    }
                };

                return new ConfigValuePart[] {new ReferencePart(commands)};
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFullReference(ConfigReferenceParser.FullReferenceContext context)
        {
            try
            {
                var commandName = context.REF_CMND_NAME().GetText();
                var commandValue = context.REF_CMND_VAL().GetText();

                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {
                        ParseCommand(SanitizeReferenceCommand(commandName)),
                        SanitizeReferenceValue(commandValue)
                    }
                };

                return new ConfigValuePart[] {new ReferencePart(commands)};
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitValueRecursive(ConfigReferenceParser.ValueRecursiveContext context)
        {
            try
            {
                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {
                        ReferenceCommand.Path,
                        SanitizeReferenceValue(context.REF_CMND_VAL()
                                                      .GetText())
                    }
                };

                return new ConfigValuePart[] {new ReferencePart(commands)}.Concat(VisitChildren(context))
                                                                          .ToArray();
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        /// <inheritdoc />
        public override ConfigValuePart[] VisitFullRecursive(ConfigReferenceParser.FullRecursiveContext context)
        {
            try
            {
                var commands = new Dictionary<ReferenceCommand, string>
                {
                    {
                        ParseCommand(SanitizeReferenceCommand(context.REF_CMND_NAME()
                                                                     .GetText())),
                        SanitizeReferenceValue(context.REF_CMND_VAL()
                                                      .GetText())
                    }
                };

                return new ConfigValuePart[] {new ReferencePart(commands)}.Concat(VisitChildren(context))
                                                                          .ToArray();
            }
            catch (Exception e)
            {
                return new ConfigValuePart[0];
            }
        }

        private readonly Dictionary<string, ReferenceCommand> _commandLookup =
            new Dictionary<string, ReferenceCommand>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"using", ReferenceCommand.Using},
                {"alias", ReferenceCommand.Alias},
                {"path", ReferenceCommand.Path},
                {"default", ReferenceCommand.Fallback},
                {"fallback", ReferenceCommand.Fallback},
            };

        private ReferenceCommand ParseCommand(string keyword)
        {
            if (_commandLookup.ContainsKey(keyword))
                return _commandLookup[keyword];

            throw new Exception($"keyword '{keyword}' not supported");
        }

        private string SanitizeReferenceCommand(string command) => command.Trim()
                                                                          .TrimEnd(':')
                                                                          .TrimEnd();

        private string SanitizeReferenceValue(string value) => TrimSurroundingQuotes(TrimTrailingSemicolon(value));

        private string TrimSurroundingQuotes(string value) => value.StartsWith("\"") &&
                                                              value.EndsWith("\"")
                                                                  ? value.TrimStart('"')
                                                                         .TrimEnd('"')
                                                                  : value;

        private string TrimTrailingSemicolon(string value) => value.TrimEnd(';');
    }
}