using System.Collections.Generic;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class DictToJsonConversionTests
    {
        public DictToJsonConversionTests()
        {
            _translator = new JsonTranslator();
        }

        private readonly IJsonTranslator _translator;

        [Fact]
        public void DeepObject()
        {
            var translated = _translator.ToJson(new Dictionary<string, string>
            {
                {"Foo/Bar/Baz", "4711"}
            });

            Assert.NotNull(translated);
            Assert.IsType<JsonDocument>(translated);
            Assert.IsType<JsonElement>(translated.RootElement.GetProperty("Foo"));
            Assert.IsType<JsonElement>(translated.RootElement.GetProperty("Foo").GetProperty("Bar"));
            Assert.IsType<JsonElement>(translated.RootElement.GetProperty("Foo").GetProperty("Bar").GetProperty("Baz"));
            Assert.Equal("4711", translated.RootElement.GetProperty("Foo").GetProperty("Bar").GetProperty("Baz").ToString());
        }

        [Fact]
        public void EmptyObject()
        {
            var translated = _translator.ToJson(new Dictionary<string, string>());

            Assert.NotNull(translated);
            Assert.IsType<JsonDocument>(translated);
            Assert.Empty(translated.RootElement.EnumerateObject());
        }

        [Fact]
        public void SimpleArray()
        {
            var translated = _translator.ToJson(new Dictionary<string, string>
            {
                {"0000", "42"},
                {"0001", "4711"}
            });

            Assert.NotNull(translated);
            Assert.IsType<JsonDocument>(translated);
            Assert.Equal("42", translated.RootElement[0].ToString());
            Assert.Equal("4711", translated.RootElement[1].ToString());
        }

        [Fact]
        public void SimpleObject()
        {
            var translated = _translator.ToJson(new Dictionary<string, string>
            {
                {"Foo", "Bar"}
            });

            Assert.NotNull(translated);
            Assert.IsType<JsonDocument>(translated);
            Assert.Equal("Bar", translated.RootElement.GetProperty("Foo").ToString());
        }
    }
}