using System.Collections.Generic;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class DictToJsonConversionTests
    {
        private readonly IJsonTranslator _translator;

        public DictToJsonConversionTests()
        {
            _translator = new JsonTranslator();
        }

        [Fact]
        public void DeepObject()
        {
            JsonElement translated = _translator.ToJson(
                new Dictionary<string, string?>
                {
                    { "Foo/Bar/Baz", "4711" }
                });

            Assert.IsType<JsonElement>(translated);
            Assert.IsType<JsonElement>(translated.GetProperty("Foo"));
            Assert.IsType<JsonElement>(translated.GetProperty("Foo").GetProperty("Bar"));
            Assert.IsType<JsonElement>(translated.GetProperty("Foo").GetProperty("Bar").GetProperty("Baz"));
            Assert.Equal("4711", translated.GetProperty("Foo").GetProperty("Bar").GetProperty("Baz").ToString());
        }

        [Fact]
        public void EmptyObject()
        {
            JsonElement translated = _translator.ToJson(new Dictionary<string, string?>());

            Assert.IsType<JsonElement>(translated);
            Assert.Empty(translated.EnumerateObject());
        }

        [Fact]
        public void SimpleArray()
        {
            JsonElement translated = _translator.ToJson(
                new Dictionary<string, string?>
                {
                    { "0000", "42" },
                    { "0001", "4711" }
                });

            Assert.IsType<JsonElement>(translated);
            Assert.Equal("42", translated[0].ToString());
            Assert.Equal("4711", translated[1].ToString());
        }

        [Fact]
        public void SimpleObject()
        {
            JsonElement translated = _translator.ToJson(
                new Dictionary<string, string?>
                {
                    { "Foo", "Bar" }
                });

            Assert.IsType<JsonElement>(translated);
            Assert.Equal("Bar", translated.GetProperty("Foo").ToString());
        }
    }
}
