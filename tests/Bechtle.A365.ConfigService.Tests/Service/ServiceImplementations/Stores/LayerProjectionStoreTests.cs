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
    public class LayerProjectionStoreTests
    {
        private readonly ILogger<LayerProjectionStore> _logger;

        public LayerProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<LayerProjectionStore>>();
        }

        [Fact]
        public async Task CreateNewEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            domainObjectManager.Setup(m => m.CreateLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("command was not passed through to DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.Create(new LayerIdentifier("Foo"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            domainObjectManager.Setup(m => m.DeleteLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("command was not passed through to DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.Delete(new LayerIdentifier("Foo"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task DeleteKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            domainObjectManager.Setup(
                                   m => m.ModifyLayerKeys(
                                       It.IsAny<LayerIdentifier>(),
                                       It.IsAny<IList<ConfigKeyAction>>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("command was not passed through to DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.DeleteKeys(new LayerIdentifier("Foo"), new[] { "Bar", "Baz" });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChild()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new EnvironmentLayer(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo/Bar", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChildSuggestion()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new EnvironmentLayer(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo/Bar/B", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new EnvironmentLayer(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), string.Empty, QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRootPart()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, long _, CancellationToken _) =>
                                   {
                                       var pathRoot = new EnvironmentLayerKeyPath("Foo");
                                       var child1 = new EnvironmentLayerKeyPath("Foo/Bar", pathRoot);
                                       pathRoot.Children.Add(child1);
                                       var child2 = new EnvironmentLayerKeyPath("Foo/Bar/Baz", child1);
                                       child1.Children.Add(child2);

                                       return Result.Success(
                                           new EnvironmentLayer(id)
                                           {
                                               KeyPaths = new List<EnvironmentLayerKeyPath> { pathRoot }
                                           });
                                   })
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKeyCompletion>> result = await store.GetKeyAutoComplete(new LayerIdentifier("Foo"), "Foo", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayers(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   Result.Success(
                                       new Page<LayerIdentifier>(
                                           new[]
                                           {
                                               new LayerIdentifier("Foo"),
                                               new LayerIdentifier("Bar")
                                           })))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<LayerIdentifier>> result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayers(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<LayerIdentifier>()))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<LayerIdentifier>> result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayers(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<LayerIdentifier>(new[] { new LayerIdentifier("Bar") })))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<LayerIdentifier>> result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjects()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "Bar", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKey>> result = await store.GetKeyObjects(
                                                     new KeyQueryParameters<LayerIdentifier>
                                                     {
                                                         Identifier = new LayerIdentifier("Foo"),
                                                         Range = QueryRange.All,
                                                         TargetVersion = -1
                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeyObjectsWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "Baz", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<DtoConfigKey>> result = await store.GetKeyObjects(
                                                     new KeyQueryParameters<LayerIdentifier>
                                                     {
                                                         Identifier = new LayerIdentifier("Foo"),
                                                         Filter = "Foo/",
                                                         RemoveRoot = "Foo",
                                                         Range = QueryRange.All,
                                                         TargetVersion = -1
                                                     });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "Bar", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string?>>> result = await store.GetKeys(
                                                                      new KeyQueryParameters<LayerIdentifier>
                                                                      {
                                                                          Identifier = new LayerIdentifier("Foo"),
                                                                          Range = QueryRange.All,
                                                                          TargetVersion = -1
                                                                      });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Bar", new EnvironmentLayerKey("Bar", "BarValue", string.Empty, string.Empty, 1) },
                                               { "Baz", new EnvironmentLayerKey("Baz", "BazValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string?>>> result = await store.GetKeys(
                                                                      new KeyQueryParameters<LayerIdentifier>
                                                                      {
                                                                          Identifier = new LayerIdentifier("Foo"),
                                                                          Range = QueryRange.Make(1, 1),
                                                                          TargetVersion = -1
                                                                      });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);
            Assert.Equal(new KeyValuePair<string, string?>("Baz", "BazValue"), result.CheckedData.Items.First());

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysPreferExactMatch()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Ba", new EnvironmentLayerKey("Foo/Ba", "FooBaValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "FooBarValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string?>>> result = await store.GetKeys(
                                                                      new KeyQueryParameters<LayerIdentifier>
                                                                      {
                                                                          Identifier = new LayerIdentifier("Foo"),
                                                                          Filter = "Foo/Ba",
                                                                          PreferExactMatch = "Foo/Ba",
                                                                          Range = QueryRange.All,
                                                                          TargetVersion = -1
                                                                      });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task GetKeysWithoutRoot()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);
            domainObjectManager.Setup(m => m.GetLayer(It.IsAny<LayerIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (LayerIdentifier id, CancellationToken _) => Result.Success(
                                       new EnvironmentLayer(id)
                                       {
                                           Keys = new Dictionary<string, EnvironmentLayerKey>
                                           {
                                               { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Ba", new EnvironmentLayerKey("Foo/Ba", "FooBaValue", string.Empty, string.Empty, 1) },
                                               { "Foo/Bar", new EnvironmentLayerKey("Foo/Bar", "FooBarValue", string.Empty, string.Empty, 1) }
                                           }
                                       }))
                               .Verifiable("Layer was not queried from DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string?>>> result = await store.GetKeys(
                                                                      new KeyQueryParameters<LayerIdentifier>
                                                                      {
                                                                          Identifier = new LayerIdentifier("Foo"),
                                                                          Filter = "Foo/",
                                                                          RemoveRoot = "Foo",
                                                                          Range = QueryRange.All,
                                                                          TargetVersion = -1
                                                                      });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);

            domainObjectManager.Verify();
        }

        [Fact]
        public async Task UpdateKeys()
        {
            var domainObjectManager = new Mock<IDomainObjectManager>(MockBehavior.Strict);

            domainObjectManager.Setup(
                                   m => m.ModifyLayerKeys(
                                       It.IsAny<LayerIdentifier>(),
                                       It.IsAny<IList<ConfigKeyAction>>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success)
                               .Verifiable("command was not passed through to DomainObjectManager");

            var store = new LayerProjectionStore(_logger, domainObjectManager.Object);

            IResult result = await store.UpdateKeys(
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
    }
}
