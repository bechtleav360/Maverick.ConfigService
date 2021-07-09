using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
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
            domainObjectManager.Setup(
                                   m => m.CreateStructure(
                                       It.IsAny<StructureIdentifier>(),
                                       It.IsAny<IDictionary<string, string>>(),
                                       It.IsAny<IDictionary<string, string>>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("Command was not passed to DomainObjectManager");

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
            domainObjectManager.Setup(
                                   m => m.ModifyStructureVariables(
                                       It.IsAny<StructureIdentifier>(),
                                       It.IsAny<IList<ConfigKeyAction>>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("Command was not passed to DomainObjectManager");

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.DeleteVariables(new StructureIdentifier("Foo", 42), new[] {"Bar"});

            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetStructures(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   Result.Success<IList<StructureIdentifier>>(
                                       new List<StructureIdentifier>
                                       {
                                           new StructureIdentifier("Foo", 42)
                                       }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructures(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>()))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructures(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   Result.Success<IList<StructureIdentifier>>(
                                       new List<StructureIdentifier>
                                       {
                                           new StructureIdentifier("Foo", 42)
                                       }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructures(It.IsAny<string>(), It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   Result.Success<IList<StructureIdentifier>>(
                                       new List<StructureIdentifier>
                                       {
                                           new StructureIdentifier("Foo", 42),
                                           new StructureIdentifier("Foo", 43),
                                       }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructure(It.IsAny<StructureIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (StructureIdentifier id, CancellationToken _) =>
                                       Result.Success(
                                           new ConfigStructure(id)
                                           {
                                               Keys = new Dictionary<string, string>
                                               {
                                                   {"Foo", "Bar"}
                                               }
                                           }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructure(It.IsAny<StructureIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (StructureIdentifier id, CancellationToken _) =>
                                       Result.Success(
                                           new ConfigStructure(id)
                                           {
                                               Keys = new Dictionary<string, string>
                                               {
                                                   {"Foo", "FooValue"},
                                                   {"Bar", "BarValue"},
                                                   {"Baz", "BazValue"},
                                               }
                                           }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetKeys(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            // expect Baz, because the data is sorted before being returned
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetVariables()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetStructure(It.IsAny<StructureIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (StructureIdentifier id, CancellationToken _) =>
                                       Result.Success(
                                           new ConfigStructure(id)
                                           {
                                               Variables = new Dictionary<string, string> {{"Foo", "FooValue"},}
                                           }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

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
            domainObjectManager.Setup(m => m.GetStructure(It.IsAny<StructureIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (StructureIdentifier id, CancellationToken _) =>
                                       Result.Success(
                                           new ConfigStructure(id)
                                           {
                                               Variables = new Dictionary<string, string>
                                               {
                                                   {"Foo", "FooValue"},
                                                   {"Bar", "BarValue"},
                                                   {"Baz", "BazValue"},
                                               }
                                           }))
                               .Verifiable("Structure was not queried from DomainObjectManager");

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.GetVariables(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            // expect Baz, because the data is sorted before being returned
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task UpdateVariables()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(
                                   m => m.ModifyStructureVariables(
                                       It.IsAny<StructureIdentifier>(),
                                       It.IsAny<IList<ConfigKeyAction>>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("Structure was not queried from DomainObjectManager");

            var store = new StructureProjectionStore(_logger, domainObjectManager.Object);

            var result = await store.UpdateVariables(new StructureIdentifier("Foo", 42), new Dictionary<string, string> {{"Bar", "Boo"}});

            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }
    }
}
