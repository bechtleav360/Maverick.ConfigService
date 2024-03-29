﻿using System;
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
    public class EnvironmentControllerTests : ControllerTests<EnvironmentController>
    {
        private readonly Mock<IJsonTranslator> _jsonTranslatorMock;

        private readonly Mock<IProjectionStore> _projectionStoreMock;
        private readonly IServiceCollection _services;

        public static IEnumerable<object?[]> InvalidIdentifierParameters => new[]
        {
            new object?[] { "", "" },
            new object?[] { null, null },
            new object?[] { "Foo", null },
            new object?[] { null, "Bar" }
        };

        public EnvironmentControllerTests()
        {
            _services = new ServiceCollection().AddLogging()
                                               .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            _projectionStoreMock = new Mock<IProjectionStore>();
            _jsonTranslatorMock = new Mock<IJsonTranslator>();
        }

        [Fact]
        public async Task AddEnvironment()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<AcceptedAtActionResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal(nameof(EnvironmentController.GetKeys), result.ActionName);
            Assert.Equal(RouteUtilities.ControllerName<EnvironmentController>(), result.ControllerName);
            Assert.Contains("version", result.RouteValues.Keys);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task AddEnvironmentProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task AddEnvironmentStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .Throws<Exception>()
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task AddEnvironmentWithoutParameters(string? category, string? name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.AddEnvironment(category!, name!));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-deletion has not been triggered");

            await TestAction<AcceptedResult>(c => c.DeleteEnvironment("Foo", "Bar"));
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteEnvironmentProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-deletion has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.DeleteEnvironment("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteEnvironmentStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .Throws<Exception>()
                                .Verifiable();

            var result = await TestAction<ObjectResult>(c => c.DeleteEnvironment("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task DeleteEnvironmentWithoutParameters(string category, string name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.DeleteEnvironment(category, name));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetEnvironments()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .ReturnsAsync(() => Result.Success(new Page<EnvironmentIdentifier>(new[] { new EnvironmentIdentifier("Foo", "Bar") })))
                                .Verifiable("environments not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetEnvironments());

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<IList<EnvironmentIdentifier>>(result.Value);
            Assert.NotEmpty((IList<EnvironmentIdentifier>)result.Value);
            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetEnvironmentsParametersForwarded()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetAvailable(QueryRange.Make(1, 2), 4711))
                                .ReturnsAsync(() => Result.Success(new Page<EnvironmentIdentifier>(new[] { new EnvironmentIdentifier("Foo", "Bar") })))
                                .Verifiable("environments not queried");

            await TestAction<OkObjectResult>(c => c.GetEnvironments(1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetEnvironmentsProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .ReturnsAsync(() => Result.Error<Page<EnvironmentIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("environments not queried");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironments());

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetEnvironmentsStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>(), It.IsAny<long>()))
                                .Throws<Exception>()
                                .Verifiable("environments not queried");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironments());

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string?>>(
                                            new Dictionary<string, string?>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetKeys("Foo", "Bar", "", "", ""));

            Assert.IsAssignableFrom<IDictionary<string, string>>(result.Value);
            Assert.NotEmpty((IDictionary<string, string>)result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJson()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string?>>(
                                            new Dictionary<string, string?>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string?>>()))
                               .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                               .Verifiable("keys not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "", "", ""));

            Assert.IsAssignableFrom<JsonElement>(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Environments.GetKeys(
                                        new KeyQueryParameters<EnvironmentIdentifier>
                                        {
                                            Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.All,
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<KeyValuePair<string, string?>>()))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string?>>>()))
                               .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                               .Verifiable("keys not translated to json");

            await TestAction<OkObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "filter", "preferExactMatch", "removeRoot", 4711));

            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string?>>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsJsonTranslationThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<KeyValuePair<string, string?>>(
                                            new Dictionary<string, string?>
                                            {
                                                { "Foo", "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            _jsonTranslatorMock.Setup(t => t.ToJson(It.IsAny<ICollection<KeyValuePair<string, string?>>>()))
                               .Throws<Exception>()
                               .Verifiable("keys not translated to json");

            var result = await TestAction<ObjectResult>(c => c.GetKeysAsJson("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjects()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeyObjects(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(
                                    () => Result.Success(
                                        new Page<DtoConfigKey>(
                                            new[]
                                            {
                                                new DtoConfigKey { Key = "Foo", Value = "Bar" }
                                            })))
                                .Verifiable("keys not queried");

            var result = await TestAction<OkObjectResult>(c => c.GetKeysWithMetadata("Foo", "Bar", "", "", ""));

            Assert.IsAssignableFrom<IEnumerable<DtoConfigKey>>(result.Value);
            Assert.NotEmpty((IEnumerable<DtoConfigKey>)result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Environments.GetKeyObjects(
                                        new KeyQueryParameters<EnvironmentIdentifier>
                                        {
                                            Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.Make(1, 2),
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<DtoConfigKey>()))
                                .Verifiable("keys not queried");

            await TestAction<OkObjectResult>(c => c.GetKeysWithMetadata("Foo", "Bar", "filter", "preferExactMatch", "removeRoot", 1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeyObjects(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<DtoConfigKey>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysWithMetadata("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysAsObjectsStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeyObjects(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeysWithMetadata("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysParametersForwarded()
        {
            _projectionStoreMock.Setup(
                                    s => s.Environments.GetKeys(
                                        new KeyQueryParameters<EnvironmentIdentifier>
                                        {
                                            Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                            Filter = "filter",
                                            PreferExactMatch = "preferExactMatch",
                                            Range = QueryRange.Make(1, 2),
                                            RemoveRoot = "removeRoot",
                                            TargetVersion = 4711
                                        }))
                                .ReturnsAsync(() => Result.Success(new Page<KeyValuePair<string, string?>>()))
                                .Verifiable("keys not queried");

            await TestAction<OkObjectResult>(c => c.GetKeys("Foo", "Bar", "filter", "preferExactMatch", "removeRoot", 1, 2, 4711));

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .ReturnsAsync(() => Result.Error<Page<KeyValuePair<string, string?>>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeys("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        [Fact]
        public async Task GetKeysStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.GetKeys(It.IsAny<KeyQueryParameters<EnvironmentIdentifier>>()))
                                .Throws<Exception>()
                                .Verifiable("keys not queried");

            var result = await TestAction<ObjectResult>(c => c.GetKeys("Foo", "Bar", "", "", ""));

            Assert.NotNull(result.Value);

            _projectionStoreMock.Verify();
        }

        protected override EnvironmentController CreateController()
        {
            ServiceProvider provider = _services.BuildServiceProvider();

            return new EnvironmentController(
                provider.GetService<ILogger<EnvironmentController>>(),
                _projectionStoreMock.Object,
                _jsonTranslatorMock.Object);
        }
    }
}
