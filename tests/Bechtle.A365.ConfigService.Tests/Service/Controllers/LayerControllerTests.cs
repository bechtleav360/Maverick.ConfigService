using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class LayerControllerTests : ControllerTests<LayerController>
    {
        private readonly Mock<IJsonTranslator> _jsonTranslatorMock;

        private readonly Mock<IProjectionStore> _projectionStoreMock;
        private readonly IServiceCollection _services;

        public static IEnumerable<object[]> InvalidIdentifierParameters => new[]
        {
            new object[] { "" },
            new object[] { null }
        };

        public static IEnumerable<object[]> InvalidKeyUpdateParameters => new[]
        {
            new object[] { null, Array.Empty<DtoConfigKey>() },
            new object[] { "", Array.Empty<DtoConfigKey>() },
            new object[] { "Foo", null },
            new object[]
            {
                "Foo", new[]
                {
                    new DtoConfigKey { Key = "Foo", Value = "Bar" },
                    new DtoConfigKey { Key = "Foo", Value = "Bar" }
                }
            }
        };

        public LayerControllerTests()
        {
            _services = new ServiceCollection().AddLogging()
                                               .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            _projectionStoreMock = new Mock<IProjectionStore>();
            _jsonTranslatorMock = new Mock<IJsonTranslator>();
        }

        [Fact]
        public async Task AddLayer()
        {
            _projectionStoreMock.Setup(s => s.Layers.Create(It.IsAny<LayerIdentifier>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<AcceptedAtActionResult>(c => c.AddLayer("Foo"));

            Assert.Equal(nameof(LayerController.GetKeys), result.ActionName);
            Assert.Equal(RouteUtilities.ControllerName<LayerController>(), result.ControllerName);
            Assert.Contains("version", result.RouteValues.Keys);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task AddLayerProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.Create(It.IsAny<LayerIdentifier>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddLayer("Foo"));

            Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task AddLayerStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.Create(It.IsAny<LayerIdentifier>()))
                                .Throws<Exception>()
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddLayer("Foo"));

            Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task AddLayerWithoutParameters(string name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.AddLayer(name));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task DeleteKeys()
        {
            _projectionStoreMock.Setup(s => s.Layers.DeleteKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<string>>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("deletion of keys not triggered");

            var result = await TestAction<AcceptedAtActionResult>(c => c.DeleteKeys("Foo", new[] { "Foo", "Bar" }));

            Assert.Equal(RouteUtilities.ControllerName<LayerController>(), result.ControllerName);
            Assert.Equal(nameof(LayerController.GetKeys), result.ActionName);
            Assert.Contains("version", result.RouteValues.Keys);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteKeysProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.DeleteKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<string>>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("deletion of keys not triggered");

            var result = await TestAction<ObjectResult>(c => c.DeleteKeys("Foo", new[] { "Foo", "Bar" }));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteKeysStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.DeleteKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<string>>()))
                                .Throws<Exception>()
                                .Verifiable("deletion of keys not triggered");

            var result = await TestAction<ObjectResult>(c => c.DeleteKeys("Foo", new[] { "Foo", "Bar" }));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [InlineData("", new string[0])]
        [InlineData(null, new string[0])]
        [InlineData("Foo", new string[0])]
        public async Task DeleteKeysWithoutParameters(string name, string[] keys)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.DeleteKeys(name, keys));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task DeleteLayer()
        {
            _projectionStoreMock.Setup(s => s.Layers.Delete(It.IsAny<LayerIdentifier>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-deletion has not been triggered");

            await TestAction<AcceptedResult>(c => c.DeleteLayer("Foo"));
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteLayerProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.Delete(It.IsAny<LayerIdentifier>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-deletion has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.DeleteLayer("Foo"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteLayerStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.Delete(It.IsAny<LayerIdentifier>()))
                                .Throws<Exception>()
                                .Verifiable();

            var result = await TestAction<ObjectResult>(c => c.DeleteLayer("Foo"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task DeleteLayerWithoutParameters(string name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.DeleteLayer(name));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetKeys()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string>>(
                                            new Dictionary<string, string>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetKeys("Foo", "Bar", "", ""));

            Assert.IsAssignableFrom<IDictionary<string, string>>(result.Value);
            Assert.NotEmpty((IDictionary<string, string>)result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJson()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string>>(
                                            new Dictionary<string, string>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                               .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                               .Verifiable("keys not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "", ""));

            Assert.IsAssignableFrom<JsonElement>(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Layers.GetKeys(
                                        new KeyQueryParameters<LayerIdentifier>
                                        {
                                            Identifier = new LayerIdentifier("Foo"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.All,
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<KeyValuePair<string, string>>()))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string>>>()))
                               .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                               .Verifiable("keys not translated to json");

            await TestAction<OkObjectResult>(c => c.GetKeysAsJson("Foo", "filter", "preferExactMatch", "removeRoot", 4711));

            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string>>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonTranslationThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string>>(
                                            new Dictionary<string, string>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string>>>()))
                               .Throws<Exception>()
                               .Verifiable("keys not translated to json");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjects()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeyObjects(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<DtoConfigKey>(
                                            new[]
                                            {
                                                new DtoConfigKey { Key = "Foo", Value = "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetKeysWithMetadata("Foo", "", "", ""));

            Assert.IsAssignableFrom<IEnumerable<DtoConfigKey>>(result.Value);
            Assert.NotEmpty((IEnumerable<DtoConfigKey>)result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Layers.GetKeyObjects(
                                        new KeyQueryParameters<LayerIdentifier>
                                        {
                                            Identifier = new LayerIdentifier("Foo"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.Make(1, 2),
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<DtoConfigKey>()))
                                .Verifiable("keys not queried");

            await TestAction<OkObjectResult>(c => c.GetKeysWithMetadata("Foo", "filter", "preferExactMatch", "removeRoot", 1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeyObjects(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<DtoConfigKey>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysWithMetadata("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeyObjects(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysWithMetadata("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Layers.GetKeys(
                                        new KeyQueryParameters<LayerIdentifier>
                                        {
                                            Identifier = new LayerIdentifier("Foo"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.Make(1, 2),
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<KeyValuePair<string, string>>()))
                                .Verifiable("keys not queried");

            await TestAction<OkObjectResult>(c => c.GetKeys("Foo", "filter", "preferExactMatch", "removeRoot", 1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string>>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeys("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetKeys(It.IsAny<KeyQueryParameters<LayerIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeys("Foo", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetLayers()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .ReturnsAsync(() => Result.Success(new Page<LayerIdentifier>(new List<LayerIdentifier> { new LayerIdentifier("Foo") })))
                                .Verifiable("environments not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetAvailableLayers());

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<IList<LayerIdentifier>>(result.Value);
            Assert.NotEmpty((IList<LayerIdentifier>)result.Value);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetLayersParametersForwarded()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetAvailable(QueryRange.Make(1, 2), 4711))
                                .ReturnsAsync(() => Result.Success(new Page<LayerIdentifier>(new List<LayerIdentifier> { new LayerIdentifier("Foo") })))
                                .Verifiable("environments not queried");

            await TestAction<OkObjectResult>(c => c.GetAvailableLayers(1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetLayersProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .ReturnsAsync(() => Result.Error<Page<LayerIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("environments not queried");

            var result = await TestAction<ObjectResult>(c => c.GetAvailableLayers());

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetLayersStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .Throws<Exception>()
                                .Verifiable("environments not queried");

            var result = await TestAction<ObjectResult>(c => c.GetAvailableLayers());

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task UpdateKeys()
        {
            _projectionStoreMock.Setup(s => s.Layers.UpdateKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<DtoConfigKey>>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("keys not updated");

            var result = await TestAction<AcceptedAtActionResult>(c => c.UpdateKeys("Foo", new[] { new DtoConfigKey { Key = "Foo", Value = "Bar" } }));

            Assert.Equal(nameof(LayerController.GetKeys), result.ActionName);
            Assert.Equal(RouteUtilities.ControllerName<LayerController>(), result.ControllerName);
            Assert.Contains("version", result.RouteValues.Keys);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidKeyUpdateParameters))]
        public async Task UpdateKeysInvalidParameters(string name, DtoConfigKey[] keys)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.UpdateKeys(name, keys));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task UpdateKeysProviderError()
        {
            _projectionStoreMock.Setup(s => s.Layers.UpdateKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<DtoConfigKey>>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("keys not updated");

            var result = await TestAction<ObjectResult>(c => c.UpdateKeys("Boo", new[] { new DtoConfigKey { Key = "Foo", Value = "Bar" } }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task UpdateKeysStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Layers.UpdateKeys(It.IsAny<LayerIdentifier>(), It.IsAny<ICollection<DtoConfigKey>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not updated");

            var result = await TestAction<ObjectResult>(c => c.UpdateKeys("Boo", new[] { new DtoConfigKey { Key = "Foo", Value = "Bar" } }));

            Assert.NotNull(result.Value);
        }

        protected override LayerController CreateController()
        {
            ServiceProvider provider = _services.BuildServiceProvider();

            return new LayerController(
                provider.GetService<ILogger<LayerController>>(),
                _projectionStoreMock.Object,
                _jsonTranslatorMock.Object);
        }
    }
}
