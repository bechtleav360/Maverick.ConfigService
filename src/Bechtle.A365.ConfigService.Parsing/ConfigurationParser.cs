using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Parsing
{
    /// <inheritdoc />
    public class ConfigurationParser : IConfigurationParser
    {
        public List<ConfigValuePart> Parse(string text)
        {
            var tokenizer = new ConfigValueTokenizer();
            var result = tokenizer.TryTokenize(text);

            // @TODO: handle error
            if (!result.HasValue)
                return new List<ConfigValuePart>();

            var parts = new List<ConfigValuePart>();
            var referenceStack = new Stack<ReferencePart>();
            var currentKeyword = ReferenceCommand.None;

            // @TODO: test / beautify this
            foreach (var token in result.Value)
            {
                switch (token.Kind)
                {
                    case ConfigValueToken.None:
                        throw new NotSupportedException($"Token-Type {nameof(ConfigValueToken.None)} not supported");

                    case ConfigValueToken.Value:
                        parts.Add(new ValuePart(token.ToStringValue()));
                        break;

                    case ConfigValueToken.CommandValue:
                    {
                        // default to 'path', to allow for concise notation in simple cases
                        if (currentKeyword == ReferenceCommand.None)
                            currentKeyword = ReferenceCommand.Path;

                        var currentReference = referenceStack.Peek();

                        // no duplicate assignments
                        if (currentReference.Commands.ContainsKey(currentKeyword))
                            throw new Exception($"could not set command '{currentKeyword}' to '{token.ToStringValue()}': command already set");

                        currentReference.Commands[currentKeyword] = token.ToStringValue()
                                                                         .Trim();

                        // reset keyword
                        currentKeyword = ReferenceCommand.None;
                        break;
                    }

                    case ConfigValueToken.CommandKeyword:
                    {
                        // ':' is included here to make tokenization easier, but we need to trim it because it's not actually helpful
                        var keyword = token.ToStringValue()
                                           .TrimEnd(':')
                                           .Trim();

                        if (keyword.Equals("using", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Using;
                        else if (keyword.Equals("alias", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Alias;
                        else if (keyword.Equals("path", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Path;
                        else if (keyword.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Fallback;
                        else if (keyword.Equals("fallback", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Fallback;
                        else
                            throw new Exception($"keyword '{keyword}' not supported");
                        break;
                    }

                    case ConfigValueToken.InstructionOpen:
                    {
                        referenceStack.Push(new ReferencePart(new Dictionary<ReferenceCommand, string>()));
                        break;
                    }

                    case ConfigValueToken.InstructionClose:
                    {
                        var finishedReference = referenceStack.Pop();

                        if (currentKeyword != ReferenceCommand.None)
                        {
                            if (finishedReference.Commands.ContainsKey(currentKeyword))
                                throw new Exception($"could not set command '{currentKeyword}' to '': command already set");
                            finishedReference.Commands[currentKeyword] = string.Empty;
                            currentKeyword = ReferenceCommand.None;
                        }

                        parts.Add(finishedReference);
                        break;
                    }

                    case ConfigValueToken.InstructionSeparator:
                    {
                        var currentReference = referenceStack.Peek();

                        if (currentKeyword != ReferenceCommand.None)
                        {
                            if (currentReference.Commands.ContainsKey(currentKeyword))
                                throw new Exception($"could not set command '{currentKeyword}' to '': command already set");
                            currentReference.Commands[currentKeyword] = string.Empty;
                        }

                        // reset keyword
                        currentKeyword = ReferenceCommand.None;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException($"Token-Type {nameof(ConfigValueToken.None)} not supported");
                }
            }

            return parts;
        }
    }
}