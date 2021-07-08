using System;
using System.Collections.Generic;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Controllers.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class ConversionControllerTests : ControllerTests<ConversionController>
    {
        private readonly Mock<IJsonTranslator> _translator = new Mock<IJsonTranslator>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override ConversionController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new ConversionController(
                provider.GetService<ILogger<ConversionController>>(),
                _translator.Object);
        }

        [Fact]
        public void DictionaryToJson()
        {
            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                       .Returns(JsonDocument.Parse("{}").RootElement)
                       .Verifiable("dictionary not translated");

            var result = TestAction<OkObjectResult>(c => c.DictionaryToJson(new Dictionary<string, string>
            {
                {"Foo:Bar", "Baz"}
            }, ":"));

            Assert.NotNull(result.Value);
            _translator.Verify();
        }

        [Fact]
        public void DictionaryToJsonNullParameter()
        {
            var result = TestAction<BadRequestObjectResult>(c => c.DictionaryToJson(null));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public void DictionaryToJsonThrows()
        {
            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                       .Throws<Exception>()
                       .Verifiable("dictionary not translated");

            var result = TestAction<ObjectResult>(c => c.DictionaryToJson(new Dictionary<string, string>
            {
                {"Foo:Bar", "Baz"}
            }, ":"));

            Assert.NotNull(result.Value);
            _translator.Verify();
        }

        [Fact]
        public void JsonToDictionary()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>(), It.IsAny<string>()))
                       .Returns(() => new Dictionary<string, string> {{"Foo", "Bar"}})
                       .Verifiable("json not translated to dictionary");

            var result = TestAction<OkObjectResult>(c => c.JsonToDictionary(JsonDocument.Parse("{\"Foo\": \"Bar\"}").RootElement));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<Dictionary<string, string>>(result.Value);
            Assert.NotEmpty((Dictionary<string, string>) result.Value);
            _translator.Verify();
        }

        [Fact]
        public void JsonToDictionaryThrows()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>(), It.IsAny<string>()))
                       .Throws<Exception>()
                       .Verifiable("json not translated to dictionary");

            var result = TestAction<ObjectResult>(c => c.JsonToDictionary(JsonDocument.Parse("{\"Foo\": \"Bar\"}").RootElement));

            Assert.NotNull(result.Value);
            _translator.Verify();
        }
    }
}