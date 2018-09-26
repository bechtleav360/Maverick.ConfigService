using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Bechtle.A365.ConfigService.Parsing
{
    /// <summary>
    ///     generate a number of tokens to represent the incoming texts
    /// </summary>
    internal class ConfigValueTokenizer : Tokenizer<ConfigValueToken>
    {
        private static readonly TextParser<TextSpan> RefOpenMatcher = Span.EqualTo("{{");

        private static readonly TextParser<TextSpan> RefCloseMatcher = Span.EqualTo("}}");

        private static readonly TextParser<char> RefSeparatorMatcher = Character.EqualTo(';');

        private static readonly TextParser<TextSpan> RefKeywordMatcher = Span.WhiteSpace
                                                                             .IgnoreMany()
                                                                             .Then(c => Span.MatchedBy(Character.Letter
                                                                                                                .AtLeastOnce()
                                                                                                                .Then(cc => Span.WhiteSpace.IgnoreMany())
                                                                                                                .Then(cc => Character.EqualTo(':'))));

        private static readonly TextParser<TextSpan> RefValueMatcher = Span.MatchedBy(Span.WhiteSpace
                                                                                          .IgnoreMany()
                                                                                          .Then(c => Character.LetterOrDigit
                                                                                                              .Or(Character.In('-', '_', '/'))
                                                                                                              .AtLeastOnce())
                                                                                          .Then(c => Span.WhiteSpace.IgnoreMany()));

        private static readonly TextParser<TextSpan> ValueMatcher = Span.WithoutAny(c => c == '{');

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
                    var fluff = ValueMatcher(lastLocation);
                    if (fluff.HasValue)
                        yield return Result.Value(ConfigValueToken.Value, lastLocation, fluff.Remainder);

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
                        yield return Result.Value(ConfigValueToken.CommandKeyword, keyword.Location, keyword.Remainder);
                        next = keyword.Remainder.ConsumeChar();
                        continue;
                    }

                    var value = RefValueMatcher(next.Location);
                    if (value.HasValue)
                    {
                        lastLocation = value.Remainder;
                        yield return Result.Value(ConfigValueToken.CommandValue, value.Location, value.Remainder);
                        next = value.Remainder.ConsumeChar();
                        continue;
                    }
                }

                next = next.Remainder.ConsumeChar();
            } while (next.HasValue);

            var remainder = ValueMatcher(lastLocation);
            if (remainder.HasValue)
                yield return Result.Value(ConfigValueToken.Value, lastLocation, remainder.Remainder);
        }

        private struct TokenizerContext
        {
            public bool InReference { get; set; }
        }
    }
}