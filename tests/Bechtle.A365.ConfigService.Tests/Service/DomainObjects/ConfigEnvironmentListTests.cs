using System;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigEnvironmentListTests
    {
        [Fact]
        public void CalculateCacheSizeEmpty() => Assert.Equal(0, new ConfigEnvironmentList().CalculateCacheSize());

        [Fact]
        public void CalculateCacheSizeFilled()
        {
            var item = new ConfigEnvironmentList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            Assert.InRange(item.CalculateCacheSize(), 0, long.MaxValue);
        }

        [Fact]
        public void CreateNew()
        {
            var item = new ConfigEnvironmentList();

            Assert.Empty(item.Identifiers);
        }

        [Fact]
        public void CreateValidSnapshot()
        {
            var item = new ConfigEnvironmentList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
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
        public void ReplayHandlesEnvironmentCreated()
        {
            var item = new ConfigEnvironmentList();

            Assert.Empty(item.Identifiers);

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            Assert.NotEmpty(item.Identifiers);
        }

        [Fact]
        public void ReplayHandlesEnvironmentDeleted()
        {
            var item = new ConfigEnvironmentList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            Assert.NotEmpty(item.Identifiers);

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentDeleted(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 2
            });

            Assert.Empty(item.Identifiers);
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new ConfigEnvironmentList();
            snapshotSource.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new ConfigEnvironmentList();
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.Identifiers, target.Identifiers);
            Assert.Equal(snapshotSource.CurrentVersion, target.CurrentVersion);
            Assert.Equal(snapshotSource.MetaVersion, target.MetaVersion);
        }
    }
}