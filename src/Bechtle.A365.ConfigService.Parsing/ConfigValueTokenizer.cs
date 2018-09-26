using System;
using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Bechtle.A365.ConfigService.Parsing
{
    public class ConfigValueParser
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

                    case ConfigValueToken.Fluff:
                        parts.Add(new FluffPart(token.ToStringValue()));
                        break;

                    case ConfigValueToken.Value:
                        // default to 'path', to allow for concise notation in simple cases
                        if (currentKeyword == ReferenceCommand.None)
                            currentKeyword = ReferenceCommand.Path;

                        var currentReference = referenceStack.Peek();

                        // no duplicate assignments
                        if (currentReference.Commands.ContainsKey(currentKeyword))
                            throw new Exception($"could not set command '{currentKeyword}' to '{token.ToStringValue()}': command already set");

                        currentReference.Commands[currentKeyword] = token.ToStringValue();

                        // reset keyword
                        currentKeyword = ReferenceCommand.None;
                        break;

                    case ConfigValueToken.Keyword:
                        // ':' is included here to make tokenization easier, but we need to trim it because it's not actually helpful
                        var keyword = token.ToStringValue()
                                           .TrimEnd(':');

                        if (keyword.Equals("using", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Using;
                        else if (keyword.Equals("alias", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Alias;
                        else if (keyword.Equals("path", StringComparison.InvariantCultureIgnoreCase))
                            currentKeyword = ReferenceCommand.Path;
                        else
                            throw new Exception($"keyword '{keyword}' not supported");
                        break;

                    case ConfigValueToken.InstructionOpen:
                        referenceStack.Push(new ReferencePart(new Dictionary<ReferenceCommand, string>()));
                        break;

                    case ConfigValueToken.InstructionClose:
                        var finishedReference = referenceStack.Pop();
                        parts.Add(finishedReference);
                        break;

                    case ConfigValueToken.InstructionSeparator:
                        // reset keyword
                        currentKeyword = ReferenceCommand.None;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException($"Token-Type {nameof(ConfigValueToken.None)} not supported");
                }
            }

            return parts;
        }
    }

    public abstract class ConfigValuePart
    {
    }

    public class FluffPart : ConfigValuePart
    {
        public string Text { get; }

        public FluffPart(string text)
        {
            Text = text;
        }
    }

    public class ReferencePart : ConfigValuePart
    {
        public Dictionary<ReferenceCommand, string> Commands { get; }

        public ReferencePart(Dictionary<ReferenceCommand, string> commands)
        {
            Commands = commands;
        }
    }

    public enum ReferenceCommand
    {
        None,
        Using,
        Alias,
        Path
    }

    /// <summary>
    ///     generate a number of tokens to represent the incoming texts
    /// </summary>
    internal class ConfigValueTokenizer : Tokenizer<ConfigValueToken>
    {
        private static readonly TextParser<TextSpan> RefOpenMatcher = Span.EqualTo("{{");

        private static readonly TextParser<TextSpan> RefCloseMatcher = Span.EqualTo("}}");

        private static readonly TextParser<char> RefSeparatorMatcher = Character.EqualTo(';');

        private static readonly TextParser<TextSpan> RefKeywordMatcher = Span.MatchedBy(Character.Letter
                                                                                                 .AtLeastOnce()
                                                                                                 .Then(c => Character.EqualTo(':')));

        private static readonly TextParser<TextSpan> RefValueMatcher = Span.MatchedBy(Character.LetterOrDigit
                                                                                               .Or(Character.In('-', '_', '/'))
                                                                                               .AtLeastOnce());

        private static readonly TextParser<TextSpan> FluffMatcher = Span.WithoutAny(c => c == '{');

        /// <inheritdoc />
        protected override IEnumerable<Result<ConfigValueToken>> Tokenize(TextSpan span, TokenizationState<ConfigValueToken> state)
        {
            var context = new TokenizerContext {InReference = false};

            var next = SkipWhiteSpace(span);
            var lastLocation = next.Location;

            if (!next.HasValue)
                yield break;

            do
            {
                var refOpen = RefOpenMatcher(next.Location);
                if (refOpen.HasValue)
                {
                    var fluff = FluffMatcher(lastLocation);
                    if (fluff.HasValue)
                        yield return Result.Value(ConfigValueToken.Fluff, lastLocation, fluff.Remainder);

                    context.InReference = true;
                    lastLocation = refOpen.Location;
                    yield return Result.Value(ConfigValueToken.InstructionOpen, refOpen.Location, refOpen.Remainder);
                    next = refOpen.Remainder.ConsumeChar();
                    continue;
                }

                if (context.InReference)
                {
                    var refClose = RefCloseMatcher(next.Location);
                    if (refClose.HasValue)
                    {
                        lastLocation = refClose.Remainder;
                        context.InReference = false;
                        yield return Result.Value(ConfigValueToken.InstructionClose, refClose.Location, refClose.Remainder);
                        next = refClose.Remainder.ConsumeChar();
                        continue;
                    }

                    var separator = RefSeparatorMatcher(next.Location);
                    if (separator.HasValue)
                    {
                        lastLocation = separator.Remainder;
                        yield return Result.Value(ConfigValueToken.InstructionSeparator, separator.Location, separator.Remainder);
                        next = separator.Remainder.ConsumeChar();
                        continue;
                    }

                    var keyword = RefKeywordMatcher(next.Location);
                    if (keyword.HasValue)
                    {
                        lastLocation = keyword.Remainder;
                        yield return Result.Value(ConfigValueToken.Keyword, keyword.Location, keyword.Remainder);
                        next = keyword.Remainder.ConsumeChar();
                        continue;
                    }

                    var value = RefValueMatcher(next.Location);
                    if (value.HasValue)
                    {
                        lastLocation = value.Remainder;
                        yield return Result.Value(ConfigValueToken.Value, value.Location, value.Remainder);
                        next = value.Remainder.ConsumeChar();
                        continue;
                    }
                }

                next = next.Remainder.ConsumeChar();
            } while (next.HasValue);

            var remainder = FluffMatcher(lastLocation);
            if (remainder.HasValue)
                yield return Result.Value(ConfigValueToken.Fluff, lastLocation, remainder.Remainder);
        }

        private struct TokenizerContext
        {
            public bool InReference { get; set; }
        }
    }
}