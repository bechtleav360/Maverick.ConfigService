using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class JsonToDictConversionTests
    {
        private readonly IJsonTranslator _translator;

        public JsonToDictConversionTests()
        {
            _translator = new JsonTranslator();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public void EmptyObject(string json)
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>(json);
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(0, translated.Count);
        }

        [Fact]
        public void EncodeSpacesWhenTold()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo With Spaces / Slashes\": \"Bar\"}");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject, true);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("Foo%20With%20Spaces%20%2F%20Slashes", translated.First().Key);
            Assert.Equal("Bar", translated.First().Value);
        }

        [Fact]
        public void LeaveSpacesByDefault()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo With Spaces / Slashes\": \"Bar\"}");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("Foo With Spaces %2F Slashes", translated.First().Key);
            Assert.Equal("Bar", translated.First().Value);
        }

        [Fact]
        public void NumberArray()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("[1,2,3]");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(3, translated.Count);
            Assert.True(translated.ContainsKey("0000"));
            Assert.True(translated.ContainsKey("0001"));
            Assert.True(translated.ContainsKey("0002"));
            Assert.Equal("1", translated["0000"]);
            Assert.Equal("2", translated["0001"]);
            Assert.Equal("3", translated["0002"]);
        }

        [Fact]
        public void PreserveNull()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"NullProp\": null}");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject, true);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("NullProp", translated.First().Key);
            Assert.Null(translated.First().Value);
        }

        [Fact]
        public void SimpleObject()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo\": \"Bar\", \"Bar\": \"Baz\", \"Baz\": { \"FooBarBaz\": \"42\"}}");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(3, translated.Count);

            Assert.True(translated.ContainsKey("Foo"));
            Assert.True(translated.ContainsKey("Bar"));
            Assert.True(translated.ContainsKey("Baz/FooBarBaz"));

            Assert.Equal("Bar", translated["Foo"]);
            Assert.Equal("Baz", translated["Bar"]);
            Assert.Equal("42", translated["Baz/FooBarBaz"]);
        }

        [Fact]
        public void SimplestObject()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Property\": \"Value\"}");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.True(translated.ContainsKey("Property"));
            Assert.Equal("Value", translated["Property"]);
        }

        [Fact]
        public void StringArray()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("[{\"Foo\": \"Bar\"}, {\"Bar\": \"Baz\"}]");
            IDictionary<string, string> translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(2, translated.Count);
            Assert.True(translated.ContainsKey("0000/Foo"));
            Assert.True(translated.ContainsKey("0001/Bar"));
            Assert.Equal("Bar", translated["0000/Foo"]);
            Assert.Equal("Baz", translated["0001/Bar"]);
        }
    }
}
