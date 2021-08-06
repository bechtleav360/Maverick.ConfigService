using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class DomainObjectStoreTests : IDisposable
    {
        private readonly Mock<IDomainObjectFileStore> _domainObjectFileStore = new Mock<IDomainObjectFileStore>(MockBehavior.Strict);

        private readonly Mock<IOptionsSnapshot<HistoryConfiguration>> _historyConfiguration =
            new Mock<IOptionsSnapshot<HistoryConfiguration>>(MockBehavior.Strict);

        private readonly IDomainObjectStoreLocationProvider _locationProvider = new TestDomainObjectStoreLocationProvider();

        private DomainObjectStore _objectStoreInstance;

        private DomainObjectStore ObjectStore => _objectStoreInstance ??= new DomainObjectStore(
                                                     new NullLoggerFactory().CreateLogger<DomainObjectStore>(),
                                                     _historyConfiguration.Object,
                                                     _locationProvider,
                                                     new MemoryCache(new MemoryCacheOptions()),
                                                     _domainObjectFileStore.Object);

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                File.Delete(_locationProvider.FileName);
            }
            catch (IOException)
            {
                // ignored on purpose
            }
        }

        [Fact]
        public async Task DomainObjectGoneAfterRemove()
        {
            var layer = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            _historyConfiguration.Setup(c => c.Value)
                                 .Returns(new HistoryConfiguration())
                                 .Verifiable("History-Configuration not retrieved");

            _domainObjectFileStore.Setup(m => m.StoreObject<EnvironmentLayer, LayerIdentifier>(It.IsAny<EnvironmentLayer>()))
                                  .ReturnsAsync(Result.Success)
                                  .Verifiable("object was not stored in local file");

            await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);

            await ObjectStore.Remove<EnvironmentLayer, LayerIdentifier>(layer.Id);

            IResult<EnvironmentLayer> result = await ObjectStore.Load<EnvironmentLayer, LayerIdentifier>(layer.Id);

            Assert.Equal(ErrorCode.NotFound, result.Code);
            Assert.NotEmpty(result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetProjectedVersion_NoEventsProjected()
        {
            IResult<long> result = await ObjectStore.GetProjectedVersion();

            AssertPositiveResult(result, -1);
        }

        [Fact]
        public async Task GetProjectedVersion_VersionSetOnce()
        {
            await ObjectStore.SetProjectedVersion(
                Guid.NewGuid().ToString("D"),
                42,
                "Fake-Event");

            IResult<long> result = await ObjectStore.GetProjectedVersion();

            AssertPositiveResult(result, 42);
        }

        [Fact]
        public async Task GetProjectedVersion_VersionSetTwice()
        {
            await ObjectStore.SetProjectedVersion(
                Guid.NewGuid().ToString("D"),
                42,
                "Fake-Event");

            await ObjectStore.SetProjectedVersion(
                Guid.NewGuid().ToString("D"),
                43,
                "Fake-Event");

            IResult<long> result = await ObjectStore.GetProjectedVersion();

            AssertPositiveResult(result, 43);
        }

        [Fact]
        public async Task ListCanBeFiltered()
        {
            var items = new List<EnvironmentLayer>
            {
                new EnvironmentLayer(new LayerIdentifier("Foo-1"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                    },
                    CurrentVersion = 42,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                },
                new EnvironmentLayer(new LayerIdentifier("Foo-2"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 43) }
                    },
                    CurrentVersion = 43,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                },
                new EnvironmentLayer(new LayerIdentifier("Foo-3"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 44) }
                    },
                    CurrentVersion = 44,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                }
            };

            foreach (EnvironmentLayer item in items)
            {
                await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(item);
            }

            IResult<Page<LayerIdentifier>> result = await ObjectStore.ListAll<EnvironmentLayer, LayerIdentifier>(
                                                        layer => layer.Name == "Foo-2",
                                                        QueryRange.All);

            AssertPositiveResult(result, new List<LayerIdentifier> { new LayerIdentifier("Foo-2") });
        }

        [Fact]
        public async Task ListPagesAreStable()
        {
            List<EnvironmentLayer> items =
                Enumerable.Range(0, 100)
                          .Select(
                              i => new EnvironmentLayer(new LayerIdentifier(Guid.NewGuid().ToString("B")))
                              {
                                  Json = "{ \"Foo\": true }",
                                  Keys = new Dictionary<string, EnvironmentLayerKey>
                                  {
                                      { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                                  },
                                  CurrentVersion = 42,
                                  KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                              })
                          .ToList();

            foreach (EnvironmentLayer item in items)
            {
                await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(item);
            }

            IResult<Page<LayerIdentifier>> result = await ObjectStore.ListAll<EnvironmentLayer, LayerIdentifier>(QueryRange.All);

            // DomainObjectStore uses LiteDb as underlying store which stores the Ids in their .ToString() representation
            // this means the result will be ordered in a way that is unaware of the numbers in the Layer-Name
            // we will get (Foo1, Foo10, Foo11) instead of (Foo1, Foo2, Foo3) as it was defined originally
            List<LayerIdentifier> expectedIds = items.Select(i => i.Id)
                                                     .OrderBy(id => id.ToString())
                                                     .ToList();

            AssertPositiveResult(result, expectedIds);
        }

        [Fact]
        public async Task ListPagesContainOnlyGivenRange()
        {
            List<EnvironmentLayer> items =
                Enumerable.Range(0, 100)
                          .Select(
                              i => new EnvironmentLayer(new LayerIdentifier(Guid.NewGuid().ToString("B")))
                              {
                                  Json = "{ \"Foo\": true }",
                                  Keys = new Dictionary<string, EnvironmentLayerKey>
                                  {
                                      { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                                  },
                                  CurrentVersion = 42,
                                  KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                              })
                          .ToList();

            foreach (EnvironmentLayer item in items)
            {
                await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(item);
            }

            // take items 26-50
            IResult<Page<LayerIdentifier>> result = await ObjectStore.ListAll<EnvironmentLayer, LayerIdentifier>(QueryRange.Make(25, 25));

            List<LayerIdentifier> expectedIds = items.Select(i => i.Id)
                                                     .OrderBy(id => id.ToString())
                                                     .Skip(25)
                                                     .Take(25)
                                                     .ToList();

            AssertPositiveResult(result, expectedIds);
        }

        [Fact]
        public async Task ListShowsAllItems()
        {
            var items = new List<EnvironmentLayer>
            {
                new EnvironmentLayer(new LayerIdentifier("Foo-1"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                    },
                    CurrentVersion = 42,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                },
                new EnvironmentLayer(new LayerIdentifier("Foo-2"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 43) }
                    },
                    CurrentVersion = 43,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                },
                new EnvironmentLayer(new LayerIdentifier("Foo-3"))
                {
                    Json = "{ \"Foo\": true }",
                    Keys = new Dictionary<string, EnvironmentLayerKey>
                    {
                        { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 44) }
                    },
                    CurrentVersion = 44,
                    KeyPaths = new List<EnvironmentLayerKeyPath> { new EnvironmentLayerKeyPath("Foo") }
                }
            };

            foreach (EnvironmentLayer item in items)
            {
                await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(item);
            }

            IResult<Page<LayerIdentifier>> result = await ObjectStore.ListAll<EnvironmentLayer, LayerIdentifier>(QueryRange.All);

            AssertPositiveResult(
                result,
                new List<LayerIdentifier>
                {
                    new LayerIdentifier("Foo-1"),
                    new LayerIdentifier("Foo-2"),
                    new LayerIdentifier("Foo-3")
                });
        }

        [Fact]
        public async Task LoadStoredDomainObject()
        {
            var layer = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            _domainObjectFileStore.Setup(s => s.LoadObject<EnvironmentLayer, LayerIdentifier>(It.IsAny<LayerIdentifier>(), It.IsAny<long>()))
                                  .ReturnsAsync(Result.Success(layer))
                                  .Verifiable("object not loaded from local file");

            await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);

            IResult<EnvironmentLayer> result = await ObjectStore.Load<EnvironmentLayer, LayerIdentifier>(layer.Id);

            AssertPositiveResult(result, layer);
        }

        [Fact]
        public async Task LoadStoredMetadata()
        {
            var domainObject = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(domainObject);

            await ObjectStore.StoreMetadata<EnvironmentLayer, LayerIdentifier>(
                domainObject,
                new Dictionary<string, string>
                {
                    { "Foo", "Bar" },
                    { "Baz", "true" }
                });

            IResult<IDictionary<string, string>> result = await ObjectStore.LoadMetadata<EnvironmentLayer, LayerIdentifier>(domainObject.Id);

            AssertPositiveResult(
                result,
                new Dictionary<string, string>
                {
                    { "Foo", "Bar" },
                    { "Baz", "true" }
                });
        }

        [Fact]
        public async Task RemoveStoredDomainObject()
        {
            var layer = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(layer);

            IResult result = await ObjectStore.Remove<EnvironmentLayer, LayerIdentifier>(layer.Id);

            AssertPositiveResult(result);
        }

        [Fact]
        public async Task SetProjectedVersion()
        {
            IResult result = await ObjectStore.SetProjectedVersion(
                                 Guid.NewGuid().ToString("D"),
                                 42,
                                 "Fake-Event");

            AssertPositiveResult(result);
        }

        [Fact]
        public async Task StoreDomainObject()
        {
            _historyConfiguration.Setup(c => c.Value)
                                 .Returns(new HistoryConfiguration())
                                 .Verifiable("History-Configuration not retrieved");

            _domainObjectFileStore.Setup(s => s.StoreObject<EnvironmentLayer, LayerIdentifier>(It.IsAny<EnvironmentLayer>()))
                                  .ReturnsAsync(Result.Success())
                                  .Verifiable("object not stored in local file");

            IResult result = await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(
                                 new EnvironmentLayer(new LayerIdentifier("Foo"))
                                 {
                                     Json = "{ \"Foo\": true }",
                                     Keys = new Dictionary<string, EnvironmentLayerKey>
                                     {
                                         { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                                     },
                                     CurrentVersion = 42,
                                     KeyPaths = new List<EnvironmentLayerKeyPath>
                                     {
                                         new EnvironmentLayerKeyPath("Foo")
                                     }
                                 });

            AssertPositiveResult(result);
        }

        [Fact]
        public async Task StoreMetadata()
        {
            var domainObject = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            _historyConfiguration.Setup(c => c.Value)
                                 .Returns(new HistoryConfiguration())
                                 .Verifiable("History-Configuration not retrieved");

            _domainObjectFileStore.Setup(s => s.StoreObject<EnvironmentLayer, LayerIdentifier>(It.IsAny<EnvironmentLayer>()))
                                  .ReturnsAsync(Result.Success)
                                  .Verifiable("object was not stored in local file");

            await ObjectStore.Store<EnvironmentLayer, LayerIdentifier>(domainObject);

            IResult result = await ObjectStore.StoreMetadata<EnvironmentLayer, LayerIdentifier>(
                                 domainObject,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" },
                                     { "Baz", "true" }
                                 });

            AssertPositiveResult(result);
        }

        [Fact]
        public async Task StoreMetadataWithoutDomainObject()
        {
            var domainObject = new EnvironmentLayer(new LayerIdentifier("Foo"))
            {
                Json = "{ \"Foo\": true }",
                Keys = new Dictionary<string, EnvironmentLayerKey>
                {
                    { "Foo", new EnvironmentLayerKey("Foo", "true", string.Empty, string.Empty, 42) }
                },
                CurrentVersion = 42,
                KeyPaths = new List<EnvironmentLayerKeyPath>
                {
                    new EnvironmentLayerKeyPath("Foo")
                }
            };

            IResult result = await ObjectStore.StoreMetadata<EnvironmentLayer, LayerIdentifier>(
                                 domainObject,
                                 new Dictionary<string, string>
                                 {
                                     { "Foo", "Bar" },
                                     { "Baz", "true" }
                                 });

            Assert.True(result.IsError, "result.IsError");
            Assert.Equal(ErrorCode.NotFound, result.Code);
        }

        private static void AssertPositiveResult(IResult result)
        {
            Assert.NotNull(result);
            Assert.False(result.IsError, "result.IsError");
        }

        private static void AssertPositiveResult<TData>(IResult<TData> result, TData expectedResult)
        {
            Assert.NotNull(result);
            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(expectedResult, result.Data);
        }

        private static void AssertPositiveResult<TData>(IResult<Page<TData>> result, IList<TData> expectedResult)
        {
            Assert.NotNull(result);
            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(expectedResult, result.Data.Items);
        }

        /// <summary>
        ///     implementation of <see cref="IDomainObjectStoreLocationProvider" /> that always points to a new file at ./data/{GUID}.db
        /// </summary>
        private class TestDomainObjectStoreLocationProvider : IDomainObjectStoreLocationProvider
        {
            private readonly Guid _fileId = Guid.NewGuid();

            /// <inheritdoc />
            public string Directory => new FileInfo(Path.Combine(Environment.CurrentDirectory, $"data/{_fileId:N}.db")).DirectoryName;

            /// <inheritdoc />
            public string FileName => new FileInfo(Path.Combine(Environment.CurrentDirectory, $"data/{_fileId:N}.db")).FullName;
        }
    }
}
