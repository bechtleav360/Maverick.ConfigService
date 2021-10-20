using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class EnvironmentProjectionStoreTests
    {
        private readonly ILogger<EnvironmentProjectionStore> _logger;

        public EnvironmentProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<EnvironmentProjectionStore>>();
        }

        [Fact]
        public async Task CreateNewEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(
                                   m => m.CreateEnvironment(
                                       It.IsAny<EnvironmentIdentifier>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success())
                               .Verifiable("Command was not passed to DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.Create(new EnvironmentIdentifier("Foo", "Bar"), false);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(
                                   m => m.DeleteEnvironment(
                                       It.IsAny<EnvironmentIdentifier>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success())
                               .Verifiable("Command was not passed to DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.Delete(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAssignedLayers()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Layers = new List<LayerIdentifier> { new LayerIdentifier("Foo") }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<LayerIdentifier>> result = await store.GetAssignedLayers(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChild()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new ConfigEnvironment(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChildSuggestion()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new ConfigEnvironment(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar/B", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new ConfigEnvironment(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(
                                                               new EnvironmentIdentifier("Foo", "Bar"),
                                                               string.Empty,
                                                               QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRootPart()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new ConfigEnvironment(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironments(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<EnvironmentIdentifier>(new[] { new EnvironmentIdentifier("Foo", "Bar") })))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<EnvironmentIdentifier>> result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironments(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<EnvironmentIdentifier>()))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<EnvironmentIdentifier>> result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironments(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<EnvironmentIdentifier>(new[] { new EnvironmentIdentifier("Foo", "Bar") })))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<EnvironmentIdentifier>> result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjects()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Bar", new EnvironmentLayerKey("Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Baz", new EnvironmentLayerKey("Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKey>> result = await store.GetKeyObjects(
                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                     {
                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                         Range = QueryRange.All
                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjectsWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Baz", new EnvironmentLayerKey("Foo/Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKey>> result = await store.GetKeyObjects(
                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                     {
                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                         Filter = "Foo/",
                                                         RemoveRoot = "Foo",
                                                         Range = QueryRange.All
                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Bar", new EnvironmentLayerKey("Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Baz", new EnvironmentLayerKey("Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string>>> result = await store.GetKeys(
                                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                                     {
                                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                                         Range = QueryRange.All
                                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Baz", new EnvironmentLayerKey("Foo/Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string>>> result = await store.GetKeys(
                                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                                     {
                                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                                         Range = QueryRange.Make(1, 1)
                                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data.Items);
            Assert.Equal(new KeyValuePair<string, string>("Foo/Bar", "BarValue"), result.Data.Items.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPreferExactMatch()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Ba", new EnvironmentLayerKey("Foo/Ba", "BaValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Baz", new EnvironmentLayerKey("Foo/Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string>>> result = await store.GetKeys(
                                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                                     {
                                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                                         Filter = "Foo/Ba",
                                                                         PreferExactMatch = "Foo/Ba",
                                                                         Range = QueryRange.All
                                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetEnvironment(It.IsAny<EnvironmentIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (EnvironmentIdentifier id, CancellationToken _) => Result.Success(
                                       new ConfigEnvironment(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Baz", new EnvironmentLayerKey("Foo/Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Environment was not queried from DomainObjectManager");

            var store = new EnvironmentProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string>>> result = await store.GetKeys(
                                                                     new KeyQueryParameters<EnvironmentIdentifier>
                                                                     {
                                                                         Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                                                                         Filter = "Foo/",
                                                                         RemoveRoot = "Foo",
                                                                         Range = QueryRange.All
                                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data.Items);

            domainObjectManager.Verify();
        }
    }
}
