using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces.Stores;
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
        private readonly Mock<IProjectionStore> _projectionStore = new Mock<IProjectionStore>();

        /// <inheritdoc />
        protected override SearchController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new SearchController(
                provider,
                provider.GetService<ILogger<SearchController>>(),
                _projectionStore.Object);
        }

        [Theory]
        [InlineData("Foo", "Bar", null, -1, -1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", -1, -1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", 1, 1, -1)]
        [InlineData("Foo", "Bar", "Foo/Bar", 1, 1, 4711)]
        public async Task GetAutocompleteParametersForwarded(string category,
                                                             string name,
                                                             string query,
                                                             int offset,
                                                             int length,
                                                             long targetVersion)
        {
            _projectionStore.Setup(s => s.Environments.GetKeyAutoComplete(new EnvironmentIdentifier(category, name),
                                                                          query,
                                                                          QueryRange.Make(offset, length),
                                                                          targetVersion))
                            .ReturnsAsync(() => Result.Success<IList<DtoConfigKeyCompletion>>(new List<DtoConfigKeyCompletion>
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
                            }))
                            .Verifiable("autocomplete-data not searched");

            await TestAction<OkObjectResult>(c => c.GetEnvironmentKeyAutocompleteList(category, name, query, offset, length, targetVersion));

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAutocomplete()
        {
            _projectionStore.Setup(s => s.Environments.GetKeyAutoComplete(It.IsAny<EnvironmentIdentifier>(),
                                                                          It.IsAny<string>(),
                                                                          It.IsAny<QueryRange>(),
                                                                          It.IsAny<long>()))
                            .ReturnsAsync(() => Result.Success<IList<DtoConfigKeyCompletion>>(new List<DtoConfigKeyCompletion>
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
                            }))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<OkObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<DtoConfigKeyCompletion>>(result.Value);
            Assert.NotEmpty((List<DtoConfigKeyCompletion>) result.Value);

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetKeyAutoComplete(It.IsAny<EnvironmentIdentifier>(),
                                                                          It.IsAny<string>(),
                                                                          It.IsAny<QueryRange>(),
                                                                          It.IsAny<long>()))
                            .ReturnsAsync(() => Result.Error<IList<DtoConfigKeyCompletion>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteStoreThrows()
        {
            _projectionStore.Setup(s => s.Environments.GetKeyAutoComplete(It.IsAny<EnvironmentIdentifier>(),
                                                                          It.IsAny<string>(),
                                                                          It.IsAny<QueryRange>(),
                                                                          It.IsAny<long>()))
                            .Throws<Exception>()
                            .Verifiable("autocomplete-data not searched");

            var result = await TestAction<ObjectResult>(c => c.GetEnvironmentKeyAutocompleteList("Foo", "Bar", "Foo/Bar"));

            Assert.NotNull(result.Value);

            _projectionStore.Verify();
        }
    }
}