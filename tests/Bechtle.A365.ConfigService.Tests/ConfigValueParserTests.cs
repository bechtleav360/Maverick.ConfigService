using System;
using Bechtle.A365.ConfigService.Parsing;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class ConfigValueParserTests
    {
        [Theory]
        [InlineData("simple, value", 1, new[] {typeof(FluffPart)})]
        [InlineData("simple, value - with some stuff", 1, new[] {typeof(FluffPart)})]
        [InlineData("{{Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] {typeof(ReferencePart)})]
        [InlineData("{{Some/Path/To/Somewhere/Other/Than/Here}} some fluff after the reference", 2, new[] {typeof(ReferencePart), typeof(FluffPart)})]
        [InlineData("this is a value: hello {{Planetoids/General}}, and these are invalid }} parts of references that: shall; not; pass", 3, new[]
        {
            typeof(FluffPart),
            typeof(ReferencePart),
            typeof(FluffPart)
        })]
        [InlineData("{{Path:Some/Path/To/Somewhere/Other/Than/Here}}", 1, new[] {typeof(ReferencePart)})]
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
    }
}