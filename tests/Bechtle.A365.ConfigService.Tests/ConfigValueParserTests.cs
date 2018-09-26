using System;
using System.Linq;
using Bechtle.A365.ConfigService.Parsing;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class ConfigValueParserTests
    {
        [Theory]
        [InlineData("simple, value", 1, new[] {typeof(ValuePart)})]
        [InlineData("simple, value - with some stuff", 1, new[] {typeof(ValuePart)})]
        [InlineData("{{Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{Some/Path/To/Somewhere/Other/Than/Here}} some fluff after the reference", 2, new[] {typeof(ReferencePart), typeof(ValuePart)})]
        [InlineData("this is a value: hello {{Planetoids/General}}, and these are invalid }} parts of references that: shall; not; pass", 3, new[]
        {
            typeof(ValuePart),
            typeof(ReferencePart),
            typeof(ValuePart)
        })]
        [InlineData("{{Path:Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{ Path:Some/Path/To/Somewhere/Other/Than/Here }}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{Path : Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{ Path : Some/Path/To/Somewhere/Other/Than/Here }}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{Using:Some/Path/To/Somewhere/Other/Than/Here;Alias:somewhereIBelong}}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{Using:Some/Path/To/Somewhere/Other/Than/Here; Alias:somewhereIBelong}}", 1, new[] {typeof(ReferencePart)})]
        public void GetReferencesFromString(string text, int expectedResults, Type[] expectedTypes)
        {
            var parser = new ConfigValueParser();
            var result = parser.Parse(text);

            Assert.NotNull(result);
            Assert.True(result.Count == expectedResults);
            Assert.True(result.Count == expectedTypes.Length);

            for (var i = 0; i < result.Count; ++i)
                Assert.IsType(expectedTypes[i], result[i]);
        }

        [Fact]
        public void ExtractCorrectReference()
        {
            var result = new ConfigValueParser().Parse("this is fluff {{Using:Handle; Alias:Secret; Path:Some/Path/To/The/Unknown}}");

            Assert.True(result.Count == 2);
            Assert.IsType<ValuePart>(result[0]);
            Assert.IsType<ReferencePart>(result[1]);

            var reference = result.OfType<ReferencePart>()
                                  .First();

            Assert.NotNull(reference);
            Assert.NotEmpty(reference.Commands);
            Assert.True(reference.Commands.Keys.Count == 3);
            Assert.True(reference.Commands[ReferenceCommand.Alias] == "Secret");
            Assert.True(reference.Commands[ReferenceCommand.Path] == "Some/Path/To/The/Unknown");
            Assert.True(reference.Commands[ReferenceCommand.Using] == "Handle");
        }
    }
}