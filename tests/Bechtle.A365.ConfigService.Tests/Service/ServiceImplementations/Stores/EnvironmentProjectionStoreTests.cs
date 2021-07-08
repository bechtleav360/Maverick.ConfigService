using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class EnvironmentProjectionStoreTests
    {
        public EnvironmentProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<EnvironmentProjectionStore>>();
        }

        private readonly ILogger<EnvironmentProjectionStore> _logger;

        [Fact]
        public async Task CreateNewEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.Create(new EnvironmentIdentifier("Foo", "Bar"), false);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.Delete(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAssignedLayers()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAssignedLayers(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChild()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChildSuggestion()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar/B", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), string.Empty, QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRootPart()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjects()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyObjects(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjectsWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyObjects(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Filter = "Foo/",
                                 RemoveRoot = "Foo",
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Range = QueryRange.Make(1, 1)
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Foo/Bar", "BarValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPreferExactMatch()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Filter = "Foo/Ba",
                                 PreferExactMatch = "Foo/Ba",
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Filter = "Foo/",
                                 RemoveRoot = "Foo",
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task ResistDuplicateKeyErrors()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<EnvironmentIdentifier>
                             {
                                 Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            Assert.Equal(3, result.Data.Count);
            Assert.Equal("fooValue", result.Data["foo"]);
            Assert.Equal("barValue", result.Data["foo/bar"]);
            Assert.Equal("bazValue", result.Data["foo/bar/baz"]);

            domainObjectManager.Verify();
        }
    }
}
