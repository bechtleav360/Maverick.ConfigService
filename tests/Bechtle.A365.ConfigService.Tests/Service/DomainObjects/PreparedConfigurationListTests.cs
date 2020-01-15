using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class PreparedConfigurationListTests
    {
        [Fact]
        public void CalculateCacheSizeEmpty() => Assert.Equal(0, new PreparedConfigurationList().CalculateCacheSize());

        [Fact]
        public void CalculateCacheSizeFilled()
        {
            var item = new PreparedConfigurationList();

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
            var item = new PreparedConfigurationList();

            Assert.Empty(item.GetIdentifiers());
        }

        [Fact]
        public void CreateValidSnapshot()
        {
            var item = new PreparedConfigurationList();

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
        public void ReplayHandlesConfigurationBuilt()
        {
            var item = new PreparedConfigurationList();

            Assert.Empty(item.GetIdentifiers());

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier("Foo", "Bar"),
                                                         new StructureIdentifier("Foo", 42), 4711),
                                                     null,
                                                     null),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            Assert.NotEmpty(item.GetIdentifiers());
        }

        [Fact]
        public void ReplayHandlesConfigurationBuiltRepeatedly()
        {
            var item = new PreparedConfigurationList();

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier("Foo", "Bar"),
                                                         new StructureIdentifier("Foo", 42), 4711),
                                                     null,
                                                     null),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            Assert.NotEmpty(item.GetIdentifiers());

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier("Foo", "Bar"),
                                                         new StructureIdentifier("Foo", 42), 4711),
                                                     null,
                                                     null),
                UtcTime = DateTime.UtcNow,
                Version = 2
            });

            Assert.Single(item.GetIdentifiers());
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new PreparedConfigurationList();
            snapshotSource.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new ConfigurationBuilt(new ConfigurationIdentifier(
                                                         new EnvironmentIdentifier("Foo", "Bar"),
                                                         new StructureIdentifier("Foo", 42), 4711),
                                                     null,
                                                     null),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new PreparedConfigurationList();
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.GetIdentifiers(), target.GetIdentifiers());
            Assert.Equal(snapshotSource.CurrentVersion, target.CurrentVersion);
            Assert.Equal(snapshotSource.MetaVersion, target.MetaVersion);
        }
    }
}