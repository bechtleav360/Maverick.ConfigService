using Bechtle.A365.ConfigService.Common.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class JsonToDictConversionTests
    {
        private IJsonTranslator Translator => new JsonTranslator();

        [Fact]
        public void SimpleObject()
        {
            var jObject = JsonConvert.DeserializeObject<JToken>("{\"Foo\": \"Bar\", \"Bar\": \"Baz\", \"Baz\": { \"FooBarBaz\": \"42\"}}");
            var translated = Translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(3, translated.Count);

            Assert.True(translated.ContainsKey("Foo"));
            Assert.True(translated.ContainsKey("Bar"));
            Assert.True(translated.ContainsKey("Baz/FooBarBaz"));

            Assert.Equal("Bar", translated["Foo"]);
            Assert.Equal("Baz", translated["Bar"]);
            Assert.Equal("42", translated["Baz/FooBarBaz"]);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public void EmptyObject(string json)
        {
            var jObject = JsonConvert.DeserializeObject<JToken>(json);
            var translated = Translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(0, translated.Count);
        }

        [Fact]
        public void EmptyString()
        {
            var jObject = JsonConvert.DeserializeObject<JToken>("");
            var translated = Translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(0, translated.Count);
        }

        [Fact]
        public void Null()
        {
            var translated = Translator.ToDictionary(null);

            Assert.NotNull(translated);
            Assert.Equal(0, translated.Count);
        }

        [Fact]
        public void SimplestObject()
        {
            var jObject = JsonConvert.DeserializeObject<JToken>("{\"Property\": \"Value\"}");
            var translated = Translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.True(translated.ContainsKey("Property"));
            Assert.Equal("Value", translated["Property"]);
        }

        [Fact]
        public void NumberArray()
        {
            var jObject = JsonConvert.DeserializeObject<JToken>("[1,2,3]");
            var translated = Translator.ToDictionary(jObject);

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
        public void StringArray()
        {
            var jObject = JsonConvert.DeserializeObject<JToken>("[{\"Foo\": \"Bar\"}, {\"Bar\": \"Baz\"}]");
            var translated = Translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(2, translated.Count);
            Assert.True(translated.ContainsKey("0000/Foo"));
            Assert.True(translated.ContainsKey("0001/Bar"));
            Assert.Equal("Bar", translated["0000/Foo"]);
            Assert.Equal("Baz", translated["0001/Bar"]);
        }
    }
}