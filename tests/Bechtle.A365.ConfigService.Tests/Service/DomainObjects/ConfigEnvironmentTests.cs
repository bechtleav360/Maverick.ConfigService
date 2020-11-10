using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("", "Bar")]
        [InlineData("Foo", "")]
        public void ThrowsForInvalidIdentifier(string category, string name) => Assert.Throws<ArgumentNullException>(
            () => new ConfigEnvironment(new EnvironmentIdentifier(category, name)));

        [Fact]
        public void CacheItemPriority() => new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")).GetCacheItemPriority();

        [Fact]
        public void CalculateCacheSize()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.NotInRange(item.CalculateCacheSize(), long.MinValue, 0);
        }

        [Fact]
        public void Create()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            Assert.True(item.Created);
            Assert.False(item.Deleted);
            Assert.False(item.IsDefault);
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void CreateAssignsValues()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            Assert.True(item.Created);
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void CreateDefault()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create(true);

            Assert.True(item.Created);
            Assert.False(item.Deleted);
            Assert.True(item.IsDefault);
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void CreateNew() => Assert.NotNull(new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")));

        [Fact]
        public void CreateSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.NotNull(item.CreateSnapshot());
        }

        [Fact]
        public void CreateValidSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("FooBar", "Bar"));
            var snapshot = item.CreateSnapshot();

            Assert.Equal(item.CurrentVersion, snapshot.Version);
            Assert.Equal(item.MetaVersion, snapshot.MetaVersion);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.Identifier));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.JsonData));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.DataType));
        }

        [Fact]
        public void Delete()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                            new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            item.Delete();

            Assert.True(item.Deleted, "item.Deleted");
            Assert.False(item.Created, "item.Created");
            Assert.Empty(item.Layers);
        }

        [Fact]
        public async Task GetKeysAsDictionaryForwardsToLayers()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(s => s.ReplayObject<EnvironmentLayer>(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710,
                                     DomainEvent = new EnvironmentLayerCreated(layer.Identifier)
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710,
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"),
                                                                                    new[] {ConfigKeyAction.Set("Jar", "Jar", "Jar", "Jar")})
                                 });

                                 return Result.Success(layer);
                             })
                             .Verifiable("Layer not retrieved");

            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });
            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"), new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                UtcTime = DateTime.UtcNow,
                Version = 2
            });

            var result = await item.GetKeysAsDictionary(domainObjectStore.Object);

            domainObjectStore.Verify();
            Assert.NotNull(result);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal("Jar", result.Data["Jar"]);
        }

        [Fact]
        public void InitializedProperties()
        {
            var identifier = new EnvironmentIdentifier("Foo", "Bar");

            var item = new ConfigEnvironment(identifier);

            Assert.NotNull(item.Layers);
            Assert.Empty(item.Layers);

            Assert.False(item.Created);
            Assert.False(item.Deleted);
            Assert.False(item.IsDefault);

            // use this comparison because we don't care about reference-equality, only value-equality
            Assert.True(identifier.Equals(item.Identifier), "identifier.Equals(item.Identifier)");
        }

        [Fact]
        public void ReplayHandlesCreate()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.True(item.Created, "item.Created");
            Assert.False(item.Deleted, "item.Deleted");
            Assert.False(item.IsDefault, "item.IsDefault");
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void ReplayHandlesCreateDefault()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.True(item.Created, "item.Created");
            Assert.False(item.Deleted, "item.Deleted");
            Assert.True(item.IsDefault, "item.IsDefault");
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void ReplayHandlesDelete()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new EnvironmentDeleted(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.False(item.Created, "item.Created");
            Assert.False(item.IsDefault, "item.IsDefault");
            Assert.True(item.Deleted, "item.Deleted");
            Assert.Empty(item.Layers);
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            snapshotSource.Create();
            snapshotSource.AssignLayers(new[] {new LayerIdentifier("Foo")});
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.Layers, target.Layers);
            Assert.Equal(snapshotSource.Identifier, target.Identifier);
            Assert.Equal(snapshotSource.Created, target.Created);
            Assert.Equal(snapshotSource.Deleted, target.Deleted);
            Assert.Equal(snapshotSource.CurrentVersion, target.CurrentVersion);
            Assert.Equal(snapshotSource.MetaVersion, target.MetaVersion);
        }

        [Fact]
        public void ThrowsForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new ConfigEnvironment(null));
    }
}