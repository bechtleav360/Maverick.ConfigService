using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class StructureIdentifierTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("foobar", "FOOBAR")]
        [InlineData("fooBar", "fooBar")]
        public void CaseInsensitivity(string string1, string string2)
            => Assert.True(
                new StructureIdentifier(string1, 42).Equals(new StructureIdentifier(string2, 42)),
                "new StructureIdentifier(string1, 42).Equals(new StructureIdentifier(string2, 42))");

        [Theory]
        [InlineData(@"{ }", "", 0)]
        [InlineData(@"{ ""name"": """", ""version"": 0 }", "", 0)]
        [InlineData(@"{ ""name"": """", ""version"": 4711 }", "", 4711)]
        [InlineData(@"{ ""name"": ""Foo"", ""version"": 0 }", "Foo", 0)]
        [InlineData(@"{ ""name"": ""Foo"", ""version"": 4711 }", "Foo", 4711)]
        public void DeserializableUsingNewtonsoft(string json, string expectedName, int expectedVersion)
        {
            var identifier = JsonConvert.DeserializeObject<StructureIdentifier>(json);

            Assert.NotNull(identifier);
            Assert.Equal(expectedName, identifier.Name);
            Assert.Equal(expectedVersion, identifier.Version);
        }

        [Theory]
        [InlineData(@"{ }", "", 0)]
        [InlineData(@"{ ""name"": """", ""version"": 0 }", "", 0)]
        [InlineData(@"{ ""name"": """", ""version"": 4711 }", "", 4711)]
        [InlineData(@"{ ""name"": ""Foo"", ""version"": 0 }", "Foo", 0)]
        [InlineData(@"{ ""name"": ""Foo"", ""version"": 4711 }", "Foo", 4711)]
        public void DeserializableUsingSystem(string json, string expectedName, int expectedVersion)
        {
            var identifier = JsonSerializer.Deserialize<StructureIdentifier>(
                json,
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });

            Assert.NotNull(identifier);
            Assert.Equal(expectedName, identifier.Name);
            Assert.Equal(expectedVersion, identifier.Version);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        [InlineData("", -1)]
        [InlineData(null, -1)]
        [InlineData(null, int.MaxValue)]
        [InlineData(null, int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData("", int.MinValue)]
        public void EmptyValues(string name, int version) => Assert.NotNull(new StructureIdentifier(name, version));

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void ExtremeVersions(int version) => Assert.NotNull(new StructureIdentifier(string.Empty, version));

        [Theory]
        [InlineData("", 42)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData(null, 42)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData("Foo", 42)]
        [InlineData("bar", -1)]
        [InlineData("Foo", int.MinValue)]
        [InlineData("Foo", int.MaxValue)]
        public void GetHashCodeStable(string name, int version)
        {
            var identifier = new StructureIdentifier(name, version);

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => identifier.GetHashCode())
                                         .ToList();

            int example = identifier.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [InlineData("", 42)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData(null, 42)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData("Foo", 42)]
        [InlineData("bar", -1)]
        [InlineData("Foo", int.MinValue)]
        [InlineData("Foo", int.MaxValue)]
        public void NoToStringExceptions(string name, int version) => Assert.NotNull(new StructureIdentifier(name, version).ToString());

        // obviously 'new object is null' is false, but this tests if the operator== has issues
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperator() => Assert.False(
            new StructureIdentifier("Foo", 42) == null,
            "new StructureIdentifier('Foo', 42) == null");

        // same behaviour as NullCheckOperator
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperatorNegated() => Assert.True(
            new StructureIdentifier("Foo", 42) != null,
            "new StructureIdentifier('Foo', 42) != null");
    }
}
