﻿using System.Linq;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class JsonToDictConversionTests
    {
        public JsonToDictConversionTests()
        {
            _translator = new JsonTranslator();
        }

        private readonly IJsonTranslator _translator;

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public void EmptyObject(string json)
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>(json);
            var translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(0, translated.Count);
        }

        [Fact]
        public void NumberArray()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("[1,2,3]");
            var translated = _translator.ToDictionary(jObject);

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
        public void SimpleObject()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo\": \"Bar\", \"Bar\": \"Baz\", \"Baz\": { \"FooBarBaz\": \"42\"}}");
            var translated = _translator.ToDictionary(jObject);

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
            var translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.True(translated.ContainsKey("Property"));
            Assert.Equal("Value", translated["Property"]);
        }

        [Fact]
        public void StringArray()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("[{\"Foo\": \"Bar\"}, {\"Bar\": \"Baz\"}]");
            var translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(2, translated.Count);
            Assert.True(translated.ContainsKey("0000/Foo"));
            Assert.True(translated.ContainsKey("0001/Bar"));
            Assert.Equal("Bar", translated["0000/Foo"]);
            Assert.Equal("Baz", translated["0001/Bar"]);
        }

        [Fact]
        public void LeaveSpacesByDefault()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo With Spaces / Slashes\": \"Bar\"}");
            var translated = _translator.ToDictionary(jObject);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("Foo With Spaces %2F Slashes", translated.First().Key);
            Assert.Equal("Bar", translated.First().Value);
        }

        [Fact]
        public void EncodeSpacesWhenTold()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"Foo With Spaces / Slashes\": \"Bar\"}");
            var translated = _translator.ToDictionary(jObject, true);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("Foo%20With%20Spaces%20%2F%20Slashes", translated.First().Key);
            Assert.Equal("Bar", translated.First().Value);
        }

        [Fact]
        public void PreserveNull()
        {
            var jObject = JsonSerializer.Deserialize<JsonElement>("{\"NullProp\": null}");
            var translated = _translator.ToDictionary(jObject, true);

            Assert.NotNull(translated);
            Assert.Equal(1, translated.Count);
            Assert.Equal("NullProp", translated.First().Key);
            Assert.Null(translated.First().Value);
        }
    }
}