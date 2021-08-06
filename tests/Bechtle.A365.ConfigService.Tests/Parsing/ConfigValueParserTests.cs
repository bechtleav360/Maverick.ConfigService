using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Parsing;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Parsing
{
    public class ConfigValueParserTests
    {
        private readonly IConfigurationParser _parser;

        /// <summary>
        ///     used for <see cref="GetReferencesFromString" />
        /// </summary>
        public static IEnumerable<object[]> ReferenceData => new[]
        {
            new object[] { "simple, value", 1, new[] { typeof(ValuePart) } },
            new object[] { "simple, value - with some stuff", 1, new[] { typeof(ValuePart) } },
            new object[] { "{{Word}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ Word }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{  Word  }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Word }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Word  }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ Word}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{  Word}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{A}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ A }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{lowercasepath}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ lowercasepath }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{camelCasePath}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ camelCasePath }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{path-with-dashes}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ path-with-dashes }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{path_with_underscores}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ path_with_underscores }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ Some/Path/To/Somewhere/Other/Than/Here }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Some/Path/To/Somewhere/Other/Than/Here}} some fluff after the reference", 2, new[] { typeof(ReferencePart), typeof(ValuePart) } },
            new object[]
            {
                "{{ Some/Path/To/Somewhere/Other/Than/Here }} some fluff after the reference", 2, new[] { typeof(ReferencePart), typeof(ValuePart) }
            },
            new object[]
            {
                "this is a value: hello {{Planetoids/General}}, and these are invalid }} parts of references that: shall; not; pass", 3, new[]
                {
                    typeof(ValuePart),
                    typeof(ReferencePart),
                    typeof(ValuePart)
                }
            },
            new object[] { "{{Path:Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ Path:Some/Path/To/Somewhere/Other/Than/Here }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Path : Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{ Path : Some/Path/To/Somewhere/Other/Than/Here }}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Using:Some/Path/To/Somewhere/Other/Than/Here;Alias:somewhereIBelong}}", 1, new[] { typeof(ReferencePart) } },
            new object[] { "{{Using:Some/Path/To/Somewhere/Other/Than/Here; Alias:somewhereIBelong}}", 1, new[] { typeof(ReferencePart) } }
        };

        public ConfigValueParserTests()
        {
            _parser = new AntlrConfigurationParser();
        }

        [Fact]
        public void ExtractCorrectReference()
        {
            List<ConfigValuePart> result = _parser.Parse("this is fluff {{Using:Handle; Alias:Secret; Path:Some/Path/To/The/Unknown}}");

            Assert.Equal(2, result.Count);
            Assert.IsType<ValuePart>(result[0]);
            Assert.IsType<ReferencePart>(result[1]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.True(reference.Commands.Keys.Count == 3);
            Assert.True(reference.Commands[ReferenceCommand.Alias] == "Secret");
            Assert.True(reference.Commands[ReferenceCommand.Path] == "Some/Path/To/The/Unknown");
            Assert.True(reference.Commands[ReferenceCommand.Using] == "Handle");
        }

        [Theory]
        [InlineData("{{Path:}}")]
        [InlineData("{{Path:;}}")]
        [InlineData("{{Fallback:}}")]
        [InlineData("{{Fallback:;}}")]
        [InlineData("{{Fallback: ;}}")]
        [InlineData("{{Fallback: ; }}")]
        public void ExtractEmptyCommands(string text)
        {
            List<ConfigValuePart> result = _parser.Parse(text);

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Empty(reference.Commands.First().Value);
        }

        [Theory]
        [InlineData("{{ Fallback: ; /Some/Path/To/The/Unknown; }}")]
        [InlineData("{{ /Some/Path/To/The/Unknown; Fallback: }}")]
        public void ExtractValueAndEmptyFallback(string text)
        {
            List<ConfigValuePart> result = _parser.Parse(text);

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
            Assert.Equal("", reference.Commands[ReferenceCommand.Fallback]);
        }

        [Theory]
        [MemberData(nameof(ReferenceData))]
        public void GetReferencesFromString(string text, int expectedResults, Type[] expectedTypes)
        {
            List<ConfigValuePart> result = _parser.Parse(text);

            Assert.NotNull(result);
            Assert.Equal(expectedResults, result.Count);
            Assert.Equal(expectedTypes.Length, result.Count);

            for (var i = 0; i < result.Count; ++i)
            {
                Assert.IsType(expectedTypes[i], result[i]);
            }
        }

        [Fact]
        public void IgnoreSingleBraces()
        {
            var input = "${longdate} ${logger} ${level} ${message}";
            List<ConfigValuePart> result = _parser.Parse(input);

            Assert.NotNull(result);
            Assert.True(result.Count == 1);
            Assert.IsType<ValuePart>(result.First());
            Assert.True(result.OfType<ValuePart>().First().Text == input);
        }

        [Fact]
        public void ParseSimpleReference()
        {
            List<ConfigValuePart> result = _parser.Parse("{{Path: Some/Path/To/The/Unknown}}");

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.True(reference.Commands.Keys.Count == 1);
            Assert.True(reference.Commands[ReferenceCommand.Path] == "Some/Path/To/The/Unknown");
        }

        [Fact]
        public void QuotedValueWithSpaces()
        {
            List<ConfigValuePart> result = _parser.Parse("{{ \"/Some/Path/To/The/Unknown\" }}");

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
        }

        [Fact]
        public void QuotedValueWithSpacesAndTrailingSemicolon()
        {
            List<ConfigValuePart> result = _parser.Parse("{{ \"/Some/Path/To/The/Unknown\"; }}");

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
        }

        [Fact]
        public void TrimPathValues()
        {
            List<ConfigValuePart> result = _parser.Parse("this is fluff {{   /Some/Path/To/The/Unknown   ;   Using: nothing;}}");

            Assert.Equal(2, result.Count);
            Assert.IsType<ValuePart>(result[0]);
            Assert.IsType<ReferencePart>(result[1]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("nothing", reference.Commands[ReferenceCommand.Using]);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
        }

        [Fact]
        public void ValueWithSpaces()
        {
            List<ConfigValuePart> result = _parser.Parse("{{ /Some/Path/To/The/Unknown }}");

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
        }

        [Fact]
        public void ValueWithSpacesAndTrailingSemicolon()
        {
            List<ConfigValuePart> result = _parser.Parse("{{ /Some/Path/To/The/Unknown; }}");

            Assert.Single(result);
            Assert.IsType<ReferencePart>(result[0]);

            ReferencePart reference = result.OfType<ReferencePart>()
                                            .FirstOrDefault();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.Equal("/Some/Path/To/The/Unknown", reference.Commands[ReferenceCommand.Path]);
        }
    }
}
