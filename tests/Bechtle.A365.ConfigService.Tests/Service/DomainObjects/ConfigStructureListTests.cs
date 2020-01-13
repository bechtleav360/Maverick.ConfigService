using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigStructureListTests
    {
        [Fact]
        public void CalculateCacheSizeEmpty() => Assert.Equal(0, new ConfigStructureList().CalculateCacheSize());

        [Fact]
        public void CalculateCacheSizeFilled()
        {
            var item = new ConfigStructureList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                   new Dictionary<string, string>(),
                                                   new Dictionary<string, string>()),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            Assert.InRange(item.CalculateCacheSize(), 0, long.MaxValue);
        }

        [Fact]
        public void CreateNew()
        {
            var item = new ConfigStructureList();

            Assert.Empty(item.Identifiers);
        }

        [Fact]
        public void CreateValidSnapshot()
        {
            var item = new ConfigStructureList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                   new Dictionary<string, string>(),
                                                   new Dictionary<string, string>()),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            var snapshot = item.CreateSnapshot();

            Assert.Equal(item.CurrentVersion, snapshot.Version);
            Assert.Equal(item.MetaVersion, snapshot.MetaVersion);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.Identifier));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.JsonData));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.DataType));
        }

        [Fact]
        public void ReplayHandlesStructureCreated()
        {
            var item = new ConfigStructureList();

            Assert.Empty(item.Identifiers);

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                   new Dictionary<string, string>(),
                                                   new Dictionary<string, string>()),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            Assert.NotEmpty(item.Identifiers);
        }

        [Fact]
        public void ReplayHandlesStructureDeleted()
        {
            var item = new ConfigStructureList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                   new Dictionary<string, string>(),
                                                   new Dictionary<string, string>()),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            Assert.NotEmpty(item.Identifiers);

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureDeleted(new StructureIdentifier("Foo", 42)),
                UtcTime = DateTime.UtcNow,
                Version = 2
            });

            Assert.Empty(item.Identifiers);
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new ConfigStructureList();
            snapshotSource.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                   new Dictionary<string, string>(),
                                                   new Dictionary<string, string>()),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new ConfigStructureList();
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.Identifiers, target.Identifiers);
            Assert.Equal(snapshotSource.CurrentVersion, target.CurrentVersion);
            Assert.Equal(snapshotSource.MetaVersion, target.MetaVersion);
        }
    }
}