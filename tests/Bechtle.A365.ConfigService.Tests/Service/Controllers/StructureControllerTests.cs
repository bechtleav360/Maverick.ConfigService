using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
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
    public class StructureControllerTests : ControllerTests<StructureController>
    {
        private readonly Mock<IProjectionStore> _projectionStore = new(MockBehavior.Strict);

        private readonly Mock<IJsonTranslator> _translator = new(MockBehavior.Strict);

        [Fact]
        public async Task AddStructure()
        {
            _projectionStore.Setup(
                                s => s.Structures.Create(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<IDictionary<string, string?>>(),
                                    It.IsAny<IDictionary<string, string?>>()))
                            .ReturnsAsync(Result.Success)
                            .Verifiable("structure not created");

            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string?> { { "Foo", "Bar" } })
                       .Verifiable("keys / variables not translated to dict");

            var result = await TestAction<AcceptedAtActionResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 42,
                                     Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.Equal(RouteUtilities.ControllerName<StructureController>(), result.ControllerName);
            Assert.Equal(nameof(StructureController.GetStructureKeys), result.ActionName);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureAcceptsArray()
        {
            _projectionStore.Setup(
                                s => s.Structures.Create(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<IDictionary<string, string?>>(),
                                    It.IsAny<IDictionary<string, string?>>()))
                            .ReturnsAsync(Result.Success)
                            .Verifiable("structure not created");

            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string?> { { "0000", "Foo" }, { "0001", "Bar" } })
                       .Verifiable("keys / variables not translated to dict");

            var result = await TestAction<AcceptedAtActionResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 42,
                                     Structure = JsonDocument.Parse("[\"Foo\",\"Bar\"]").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.Equal(RouteUtilities.ControllerName<StructureController>(), result.ControllerName);
            Assert.Equal(nameof(StructureController.GetStructureKeys), result.ActionName);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = string.Empty,
                                     Version = 1,
                                     Structure = JsonDocument.Parse("{}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 0,
                                     Structure = JsonDocument.Parse("{}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureNoBody()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 42,
                                     Structure = JsonDocument.Parse("{}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureNoMetadata()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.AddStructure(null));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureProviderError()
        {
            _projectionStore.Setup(
                                s => s.Structures.Create(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<IDictionary<string, string?>>(),
                                    It.IsAny<IDictionary<string, string?>>()))
                            .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                            .Verifiable("structure not created");

            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string?> { { "Foo", "Bar" } })
                       .Verifiable("keys / variables not translated to dict");

            var result = await TestAction<ObjectResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 42,
                                     Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task AddStructureStoreThrows()
        {
            _projectionStore.Setup(
                                s => s.Structures.Create(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<IDictionary<string, string?>>(),
                                    It.IsAny<IDictionary<string, string?>>()))
                            .Throws<Exception>()
                            .Verifiable("structure not created");

            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string?> { { "Foo", "Bar" } })
                       .Verifiable("keys / variables not translated to dict");

            var result = await TestAction<ObjectResult>(
                             c => c.AddStructure(
                                 new DtoStructure
                                 {
                                     Name = "Foo",
                                     Version = 42,
                                     Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                                     Variables = new Dictionary<string, object>()
                                 }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetAvailableStructures()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<StructureIdentifier>(
                                        new List<StructureIdentifier>
                                        {
                                            new("Foo", 42)
                                        })))
                            .Verifiable("available structures not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetStructures());

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<Dictionary<string, int[]>>(result.Value);
            Assert.NotEmpty((Dictionary<string, int[]>)result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAvailableStructuresProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<Page<StructureIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available structures not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructures());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAvailableStructureStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("available structures not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructures());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetStructureJson()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<KeyValuePair<string, string?>>(
                                        new Dictionary<string, string?>
                                        {
                                            { "Foo", "Bar" }
                                        })))
                            .Verifiable("structure keys not retrieved");

            _translator.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string?>>>()))
                       .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                       .Verifiable("keys not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.GetStructureJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureJsonInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetStructureJson("", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureJsonInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetStructureJson("Foo", -1));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureJsonProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string?>>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructureJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureJsonStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructureJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureJsonTranslatorError()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<KeyValuePair<string, string?>>(
                                        new Dictionary<string, string?>
                                        {
                                            { "Foo", "Bar" }
                                        })))
                            .Verifiable("structure keys not retrieved");

            _translator.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string?>>>()))
                       .Returns(() => JsonDocument.Parse("null").RootElement)
                       .Verifiable("keys not translated to json");

            var result = await TestAction<ObjectResult>(c => c.GetStructureJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureKeys()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<KeyValuePair<string, string?>>(
                                        new Dictionary<string, string?>
                                        {
                                            { "Foo", "Bar" }
                                        })))
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetStructureKeys("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureKeysInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetStructureKeys("", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureKeysInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetStructureKeys("Foo", -1));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureKeysProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string?>>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructureKeys("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetStructureKeysStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStructureKeys("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetVariables()
        {
            _projectionStore.Setup(
                                s => s.Structures.GetVariables(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<KeyValuePair<string, string?>>(
                                        new Dictionary<string, string?>
                                        {
                                            { "Foo", "Bar" }
                                        })))
                            .Verifiable("variables not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetVariables("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetVariables("", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetVariables("Foo", -1));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesJson()
        {
            _projectionStore.Setup(
                                s => s.Structures.GetVariables(
                                    It.IsAny<StructureIdentifier>(),
                                    It.IsAny<QueryRange>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<KeyValuePair<string, string?>>(
                                        new Dictionary<string, string?>
                                        {
                                            { "Foo", "Bar" }
                                        })))
                            .Verifiable("variables not retrieved");

            _translator.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string?>>>()))
                       .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                       .Verifiable("keys not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.GetVariablesJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
            _translator.Verify();
        }

        [Fact]
        public async Task GetVariablesJsonInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetVariablesJson("", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesJsonInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetVariablesJson("Foo", -1));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesJsonProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string?>>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("variables not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetVariablesJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesJsonStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("variables not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetVariablesJson("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVariablesStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("variables not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetVariables("Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task RemoveVariables()
        {
            _projectionStore.Setup(s => s.Structures.DeleteVariables(It.IsAny<StructureIdentifier>(), It.IsAny<ICollection<string>>()))
                            .ReturnsAsync(Result.Success)
                            .Verifiable("variables not removed");

            var result = await TestAction<AcceptedAtActionResult>(c => c.RemoveVariables("Foo", 42, new[] { "Foo" }));

            Assert.Equal(RouteUtilities.ControllerName<StructureController>(), result.ControllerName);
            Assert.Equal(nameof(StructureController.GetVariables), result.ActionName);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task RemoveVariablesEmptyList()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.RemoveVariables("Foo", 42, Array.Empty<string>()));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RemoveVariablesInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.RemoveVariables("", 42, new[] { "Foo" }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RemoveVariablesInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.RemoveVariables("Foo", 0, new[] { "Foo" }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RemoveVariablesProviderError()
        {
            _projectionStore.Setup(s => s.Structures.DeleteVariables(It.IsAny<StructureIdentifier>(), It.IsAny<ICollection<string>>()))
                            .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                            .Verifiable("variables not removed");

            var result = await TestAction<ObjectResult>(c => c.RemoveVariables("Foo", 42, new[] { "Foo" }));

            Assert.NotNull(result);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task RemoveVariablesStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.DeleteVariables(It.IsAny<StructureIdentifier>(), It.IsAny<ICollection<string>>()))
                            .Throws<Exception>()
                            .Verifiable("variables not removed");

            var result = await TestAction<ObjectResult>(c => c.RemoveVariables("Foo", 42, new[] { "Foo" }));

            Assert.NotNull(result);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task UpdateVariables()
        {
            _projectionStore.Setup(s => s.Structures.UpdateVariables(It.IsAny<StructureIdentifier>(), It.IsAny<IDictionary<string, string>>()))
                            .ReturnsAsync(Result.Success)
                            .Verifiable("variables not updated");

            var result = await TestAction<AcceptedAtActionResult>(
                             c => c.UpdateVariables(
                                 "Foo",
                                 42,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" }
                                 }));

            Assert.Equal(RouteUtilities.ControllerName<StructureController>(), result.ControllerName);
            Assert.Equal(nameof(StructureController.GetVariables), result.ActionName);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task UpdateVariablesEmptyList()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.UpdateVariables("Foo", 42, new Dictionary<string, string>()));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task UpdateVariablesInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.UpdateVariables(
                                 "",
                                 42,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task UpdateVariablesInvalidVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.UpdateVariables(
                                 "Foo",
                                 0,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task UpdateVariablesProviderError()
        {
            _projectionStore.Setup(s => s.Structures.UpdateVariables(It.IsAny<StructureIdentifier>(), It.IsAny<IDictionary<string, string>>()))
                            .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                            .Verifiable("variables not removed");

            var result = await TestAction<ObjectResult>(
                             c => c.UpdateVariables(
                                 "Foo",
                                 42,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" }
                                 }));

            Assert.NotNull(result);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task UpdateVariablesStoreThrows()
        {
            _projectionStore.Setup(s => s.Structures.UpdateVariables(It.IsAny<StructureIdentifier>(), It.IsAny<IDictionary<string, string>>()))
                            .Throws<Exception>()
                            .Verifiable("variables not removed");

            var result = await TestAction<ObjectResult>(
                             c => c.UpdateVariables(
                                 "Foo",
                                 42,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" }
                                 }));

            Assert.NotNull(result);
            _projectionStore.Verify();
        }

        /// <inheritdoc />
        protected override StructureController CreateController()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                                         .Build();

            ServiceProvider provider = new ServiceCollection().AddLogging()
                                                              .AddSingleton<IConfiguration>(configuration)
                                                              .BuildServiceProvider();

            return new StructureController(
                provider.GetService<ILogger<StructureController>>(),
                _projectionStore.Object,
                _translator.Object);
        }
    }
}
