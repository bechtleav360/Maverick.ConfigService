using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
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
    public class SearchControllerTests : ControllerTests<SearchController>
    {
        private readonly Mock<IProjectionStore> _projectionStore = new();

        [Fact]
        public async Task GetEnvAutocomplete()
        {
            _projectionStore.Setup(
                                s => s.Environments.GetKeyAutoComplete(
                                    It.IsAny<EnvironmentIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<DtoConfigKeyCompletion>(
                                        new List<DtoConfigKeyCompletion>
                                        {
                                            new()
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Baz",
                                                HasChildren = false
                                            },
                                            new()
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Que",
                                                HasChildren = true
                                            }
                                        })))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<OkObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<DtoConfigKeyCompletion>>(result.Value);
            Assert.NotEmpty((List<DtoConfigKeyCompletion>)result.Value);

            _projectionStore.Verify();
        }

        [Theory]
        [InlineData("Foo", "Bar", null, -1, -1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", -1, -1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", 1, 1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", 1, 1, 4711)]
        public async Task GetEnvAutocompleteParametersForwarded(
            string category,
            string name,
            string query,
            int offset,
            int length,
            long targetVersion)
        {
            _projectionStore.Setup(
                                s => s.Environments.GetKeyAutoComplete(
                                    new EnvironmentIdentifier(category, name),
                                    query,
                                    QueryRange.Make(offset, length),
                                    targetVersion))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<DtoConfigKeyCompletion>(
                                        new[]
                                        {
                                            new DtoConfigKeyCompletion
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Baz",
                                                HasChildren = false
                                            },
                                            new DtoConfigKeyCompletion
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Que",
                                                HasChildren = true
                                            }
                                        })))
                            .Verifiable("autocomplete-data not searched");

            await TestAction<OkObjectResult>(c => c.GetEnvironmentKeyAutocompleteList(category, name, query, offset, length, targetVersion));

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetEnvAutocompleteProviderError()
        {
            _projectionStore.Setup(
                                s => s.Environments.GetKeyAutoComplete(
                                    It.IsAny<EnvironmentIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .ReturnsAsync(() => Result.Error<Page<DtoConfigKeyCompletion>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetEnvAutocompleteStoreThrows()
        {
            _projectionStore.Setup(
                                s => s.Environments.GetKeyAutoComplete(
                                    It.IsAny<EnvironmentIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .Throws<Exception>()
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetLayerAutocomplete()
        {
            _projectionStore.Setup(
                                s => s.Layers.GetKeyAutoComplete(
                                    It.IsAny<LayerIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<DtoConfigKeyCompletion>(
                                        new List<DtoConfigKeyCompletion>
                                        {
                                            new()
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Baz",
                                                HasChildren = false
                                            },
                                            new()
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Que",
                                                HasChildren = true
                                            }
                                        })))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<OkObjectResult>(c => c.GetLayerKeyAutocompleteList("Foo", "Foo/Bar"));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<DtoConfigKeyCompletion>>(result.Value);
            Assert.NotEmpty((List<DtoConfigKeyCompletion>)result.Value);

            _projectionStore.Verify();
        }

        [Theory]
        [InlineData("Foo", null, -1, -1, -1)]
        [InlineData("Foo", "Foo/Bar", -1, -1, -1)]
        [InlineData("Foo", "Foo/Bar", 1, 1, -1)]
        [InlineData("Foo", "Foo/Bar", 1, 1, 4711)]
        public async Task GetLayerAutocompleteParametersForwarded(
            string name,
            string query,
            int offset,
            int length,
            long targetVersion)
        {
            _projectionStore.Setup(
                                s => s.Layers.GetKeyAutoComplete(
                                    new LayerIdentifier(name),
                                    query,
                                    QueryRange.Make(offset, length),
                                    targetVersion))
                            .ReturnsAsync(
                                () => Result.Success(
                                    new Page<DtoConfigKeyCompletion>(
                                        new[]
                                        {
                                            new DtoConfigKeyCompletion
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Baz",
                                                HasChildren = false
                                            },
                                            new DtoConfigKeyCompletion
                                            {
                                                FullPath = "Foo/Bar",
                                                Completion = "Que",
                                                HasChildren = true
                                            }
                                        })))
                            .Verifiable("autocomplete-data not searched");

            await TestAction<OkObjectResult>(c => c.GetLayerKeyAutocompleteList(name, query, offset, length, targetVersion));

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetLayerAutocompleteProviderError()
        {
            _projectionStore.Setup(
                                s => s.Layers.GetKeyAutoComplete(
                                    It.IsAny<LayerIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .ReturnsAsync(() => Result.Error<Page<DtoConfigKeyCompletion>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetLayerKeyAutocompleteList("Foo", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetLayerAutocompleteStoreThrows()
        {
            _projectionStore.Setup(
                                s => s.Layers.GetKeyAutoComplete(
                                    It.IsAny<LayerIdentifier>(),
                                    It.IsAny<string>(),
                                    It.IsAny<QueryRange>(),
                                    It.IsAny<long>()))
                            .Throws<Exception>()
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetLayerKeyAutocompleteList("Foo", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }

        /// <inheritdoc />
        protected override SearchController CreateController()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                                         .Build();

            ServiceProvider provider = new ServiceCollection().AddLogging()
                                                              .AddSingleton<IConfiguration>(configuration)
                                                              .BuildServiceProvider();

            return new SearchController(
                provider.GetService<ILogger<SearchController>>(),
                _projectionStore.Object);
        }
    }
}
