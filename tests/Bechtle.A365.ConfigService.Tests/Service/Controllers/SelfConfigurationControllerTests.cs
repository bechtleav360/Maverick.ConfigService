using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Controllers.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public sealed class SelfConfigurationControllerTests : ControllerTests<SelfConfigurationController>, IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                if (File.Exists(ConfigFileLocation))
                    File.Delete(ConfigFileLocation);
            }
            catch (IOException)
            {
                // intentionally left empty
            }
        }

        private readonly IJsonTranslator _translator = new JsonTranslator();
        private const string ConfigFileLocation = "data/appsettings.json";

        protected override SelfConfigurationController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddMetrics()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new SelfConfigurationController(
                provider,
                provider.GetService<ILogger<SelfConfigurationController>>(),
                _translator);
        }

        [Fact]
        public async Task AppendNestedObject()
        {
            var first = JsonSerializer.Serialize(new
            {
                Foo = "FooValue42"
            }, new JsonSerializerOptions {WriteIndented = true});
            var firstJson = JsonDocument.Parse(first).RootElement;
            var second = JsonSerializer.Serialize(new
            {
                Bar = new {Baz = 42}
            }, new JsonSerializerOptions {WriteIndented = true});
            var secondJson = JsonDocument.Parse(second).RootElement;
            var result = JsonSerializer.Serialize(new
            {
                Bar = new {Baz = 42},
                Foo = "FooValue42"
            }, new JsonSerializerOptions {WriteIndented = true});

            await TestAction<OkResult>(c => c.AppendConfiguration(firstJson));
            await TestAction<OkResult>(c => c.AppendConfiguration(secondJson));

            await using var file = File.OpenRead(ConfigFileLocation);
            var actual = await JsonSerializer.DeserializeAsync<JsonElement>(file);

            Assert.Equal(result, actual.ToString());
        }

        [Fact]
        public async Task AppendOnce()
        {
            var expected = JsonSerializer.Serialize(new {Foo = "Bar"}, new JsonSerializerOptions {WriteIndented = true});
            var json = JsonDocument.Parse(expected).RootElement;

            await TestAction<OkResult>(c => c.AppendConfiguration(json));

            var actual = File.ReadAllText(ConfigFileLocation, Encoding.UTF8);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task DumpExistingFile()
        {
            var expectedFile = JsonSerializer.Serialize(new {Foo = "Bar"});

            File.WriteAllText(ConfigFileLocation, expectedFile);

            var result = await TestAction<OkObjectResult>(c => c.DumpConfiguration());

            Assert.Equal(expectedFile, result.Value.ToString());
        }
    }
}