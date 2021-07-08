using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class LayerProjectionStoreTests
    {
        public LayerProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<LayerProjectionStore>>();
        }

        private readonly ILogger<LayerProjectionStore> _logger;

        [Fact]
        public async Task CreateNewEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.Create(new LayerIdentifier("Foo"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.Delete(new LayerIdentifier("Foo"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.DeleteKeys(new LayerIdentifier("Foo"), new[] {"Bar", "Baz"});

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChild()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo/Bar", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChildSuggestion()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo/Bar/B", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), string.Empty, QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRootPart()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyObjects(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeyObjects(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
                                 Range = QueryRange.Make(1, 1)
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPreferExactMatch()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
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

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
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
        public async Task UpdateKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.UpdateKeys(
                             new LayerIdentifier("Foo"),
                             new[]
                             {
                                 new DtoConfigKey
                                 {
                                     Description = "description",
                                     Key = "Foo",
                                     Type = "type",
                                     Value = "foovalue"
                                 }
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task ResistDuplicateKeyErrors()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(
                             new KeyQueryParameters<LayerIdentifier>
                             {
                                 Identifier = new LayerIdentifier("Foo"),
                                 Range = QueryRange.All
                             });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            Assert.Single(result.Data.Keys);

            domainObjectManager.Verify();
        }
    }
}
