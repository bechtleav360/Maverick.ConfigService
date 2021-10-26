using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class LayerIdentifierTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("name", "NAME")]
        [InlineData("fooBar", "fOObAR")]
        public void CaseInsensitivity(string nameLeft, string nameRight)
            => Assert.True(
                new LayerIdentifier(nameLeft).Equals(new LayerIdentifier(nameRight)),
                "new LayerIdentifier(nameLeft).Equals(new LayerIdentifier(nameRight))");

        [Theory]
        [InlineData(@"{ }", "")]
        [InlineData(@"{ ""name"": """" }", "")]
        [InlineData(@"{ ""name"": ""Bar"" }", "Bar")]
        public void DeserializableUsingNewtonsoft(string json, string expectedName)
        {
            var identifier = JsonConvert.DeserializeObject<LayerIdentifier>(json);

            Assert.NotNull(identifier);
            Assert.Equal(expectedName, identifier.Name);
        }

        [Theory]
        [InlineData(@"{ }", "")]
        [InlineData(@"{ ""name"": """" }", "")]
        [InlineData(@"{ ""name"": ""Bar"" }", "Bar")]
        public void DeserializableUsingSystem(string json, string expectedName)
        {
            var identifier = JsonSerializer.Deserialize<LayerIdentifier>(
                json,
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                });

            Assert.NotNull(identifier);
            Assert.Equal(expectedName, identifier!.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Foo")]
        public void EmptyValues(string name) => Assert.NotNull(new LayerIdentifier(name));

        [Theory]
        [InlineData("")]
        [InlineData("FooBar")]
        public void GetHashCodeStable(string name)
        {
            var identifier = new LayerIdentifier(name);

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(_ => identifier.GetHashCode())
                                         .ToList();

            int example = identifier.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h=>h==example)");
        }

        [Theory]
        [InlineData("")]
        [InlineData("Foo")]
        public void NoToStringExceptions(string name) => Assert.NotNull(new LayerIdentifier(name).ToString());

        // obviously 'new object is null' is false, but this tests if the operator== has issues
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperator() => Assert.False(
            new LayerIdentifier("Foo") == null,
            "new LayerIdentifier('Foo') == null");

        // same behaviour as NullCheckOperator
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [Fact]
        public void NullCheckOperatorNegated() => Assert.True(
            new LayerIdentifier("Foo") != null,
            "new LayerIdentifier('Foo') != null");
    }
}
