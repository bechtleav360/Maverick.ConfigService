using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.Converters;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class DictToJsonConversionTests
    {
        private static IJsonTranslator Translator => new JsonTranslator();

        [Fact]
        public void SimpleObject()
        {
            var translated = Translator.ToJson(new Dictionary<string, string>
            {
                {"Foo", "Bar"},
            });

            Assert.NotNull(translated);
            Assert.IsType<JObject>(translated);
            Assert.Equal("Bar", translated["Foo"]);
        }

        [Fact]
        public void SimpleArray()
        {
            var translated = Translator.ToJson(new Dictionary<string, string>
            {
                {"0000", "42"},
                {"0001", "4711"},
            });

            Assert.NotNull(translated);
            Assert.IsType<JArray>(translated);
            Assert.Equal("42", translated[0].ToString());
            Assert.Equal("4711", translated[1].ToString());
        }

        [Fact]
        public void DeepObject()
        {
            var translated = Translator.ToJson(new Dictionary<string, string>
            {
                {"Foo/Bar/Baz/One/Two/Three/Office", "4711"}
            });

            Assert.NotNull(translated);
            Assert.IsType<JObject>(translated);
            Assert.IsType<JObject>(translated["Foo"]);
            Assert.IsType<JObject>(translated["Foo"]["Bar"]);
            Assert.IsType<JObject>(translated["Foo"]["Bar"]["Baz"]);
            Assert.IsType<JObject>(translated["Foo"]["Bar"]["Baz"]["One"]);
            Assert.IsType<JObject>(translated["Foo"]["Bar"]["Baz"]["One"]["Two"]);
            Assert.IsType<JObject>(translated["Foo"]["Bar"]["Baz"]["One"]["Two"]["Three"]);
            Assert.IsType<JValue>(translated["Foo"]["Bar"]["Baz"]["One"]["Two"]["Three"]["Office"]);
            Assert.Equal("4711", translated["Foo"]["Bar"]["Baz"]["One"]["Two"]["Three"]["Office"].ToString());
        }

        [Fact]
        public void EmptyObject()
        {
            var translated = Translator.ToJson(new Dictionary<string, string>());

            Assert.NotNull(translated);
            Assert.IsType<JObject>(translated);
            Assert.False(translated.HasValues);
            Assert.Empty(translated.Children());
        }
    }
}