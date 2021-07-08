using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class StructureProjectionStoreTests
    {
        public StructureProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<StructureProjectionStore>>();
        }

        private readonly ILogger<StructureProjectionStore> _logger;

        [Fact]
        public async Task CreateNewStructure()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.Create(
                             new StructureIdentifier("Foo", 42),
                             new Dictionary<string, string> {{"Foo", "Bar"}},
                             new Dictionary<string, string> {{"Bar", "Baz"}});

            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteVariables()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.DeleteVariables(new StructureIdentifier("Foo", 42), new[] {"Bar"});

            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailableVersions()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetAvailableVersions("Foo", QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(2, result.Data.Count);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(new StructureIdentifier("Foo", 42), QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetVariables()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetVariables(new StructureIdentifier("Foo", 42), QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetVariablesPaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetVariables(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task UpdateVariables()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.UpdateVariables(new StructureIdentifier("Foo", 42), new Dictionary<string, string> {{"Bar", "Boo"}});

            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }
    }
}
