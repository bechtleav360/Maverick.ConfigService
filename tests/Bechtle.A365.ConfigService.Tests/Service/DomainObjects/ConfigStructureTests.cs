using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class ConfigStructureTests
    {
        [Theory]
        [InlineData("FooBar", 0)]
        [InlineData("FooBar", -1)]
        [InlineData("FooBar", int.MinValue)]
        [InlineData(null, int.MaxValue)]
        [InlineData(null, 1)]
        [InlineData(null, 0)]
        [InlineData(null, -1)]
        [InlineData(null, int.MinValue)]
        [InlineData("", int.MaxValue)]
        [InlineData("", 1)]
        [InlineData("", 0)]
        [InlineData("", -1)]
        [InlineData("", int.MinValue)]
        public void ThrowsForInvalidIdentifier(string name, int version) => Assert.Throws<ArgumentNullException>(
            () => new ConfigStructure(new StructureIdentifier(name, version)));

        [Fact]
        public void CalculateCacheSize()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            Assert.NotInRange(item.CalculateCacheSize(), long.MinValue, 0);
        }

        [Fact]
        public void CreateAssignsValues()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.Create(new Dictionary<string, string> {{"Foo", "Bar"}},
                        new Dictionary<string, string> {{"Bar", "Baz"}});

            Assert.True(item.Created);
            Assert.NotEmpty(item.Keys);
            Assert.NotEmpty(item.Variables);
        }

        [Fact]
        public void CreateNew() => Assert.NotNull(new ConfigStructure(new StructureIdentifier("Foo", 42)));

        [Fact]
        public void CreateSnapshot()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            Assert.NotNull(item.CreateSnapshot());
        }

        [Fact]
        public void CreateValidSnapshot()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));
            var snapshot = item.CreateSnapshot();

            Assert.Equal(item.CurrentVersion, snapshot.Version);
            Assert.Equal(item.MetaVersion, snapshot.MetaVersion);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.Identifier));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.JsonData));
            Assert.False(string.IsNullOrWhiteSpace(snapshot.DataType));
        }

        [Fact]
        public void DeleteVariablesChangesValues()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.ModifyVariables(new Dictionary<string, string> {{"Foo", "Bar"}});

            Assert.NotEmpty(item.Variables);

            item.DeleteVariables(new List<string> {"Foo"});

            Assert.Empty(item.Variables);
        }

        [Fact]
        public void DeletingEmptyListFails()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            var result = item.DeleteVariables(new List<string>());

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void DeletingInvalidVariablesFails()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.ModifyVariables(new Dictionary<string, string> {{"Foo", "Bar"}});

            Assert.NotEmpty(item.Variables);

            var result = item.DeleteVariables(new List<string> {"Baz"});

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void DeletingNullListFails()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            var result = item.DeleteVariables(null);

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void InitializedProperties()
        {
            var identifier = new StructureIdentifier("FooBar", 42);

            var item = new ConfigStructure(identifier);

            Assert.NotNull(item.Keys);
            Assert.Empty(item.Keys);

            Assert.NotNull(item.Variables);
            Assert.Empty(item.Variables);

            Assert.False(item.Created);
            Assert.False(item.Deleted);

            // use this comparison because we don't care about reference-equality, only value-equality
            Assert.True(identifier.Equals(item.Identifier), "identifier.Equals(item.Identifier)");
        }

        [Fact]
        public void ModifyingEmptyListFails()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            var result = item.ModifyVariables(new Dictionary<string, string>());

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void ModifyingNullListFails()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            var result = item.ModifyVariables(null);

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void ModifyVariablesChangesValues()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            Assert.Empty(item.Variables);

            item.ModifyVariables(new Dictionary<string, string> {{"Foo", "Bar"}});

            Assert.NotEmpty(item.Variables);
        }

        [Fact]
        public void OverwriteExistingVariables()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.ModifyVariables(new Dictionary<string, string> {{"Foo", "Bar"}});
            item.ModifyVariables(new Dictionary<string, string> {{"Foo", "Baz"}});

            Assert.Equal("Baz", item.Variables["Foo"]);
        }

        [Fact]
        public void ReplayHandlesCreate()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new StructureCreated(
                    new StructureIdentifier("FooBar", 42),
                    new Dictionary<string, string> {{"Foo", "Bar"}},
                    new Dictionary<string, string> {{"Bar", "Baz"}}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.True(item.Created, "item.Created");
            Assert.False(item.Deleted, "item.Deleted");
            Assert.Equal("Bar", item.Keys["Foo"]);
            Assert.Equal("Baz", item.Variables["Bar"]);
        }

        [Fact]
        public void ReplayHandlesDelete()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.Create(new Dictionary<string, string> {{"Foo", "Bar"}},
                        new Dictionary<string, string> {{"Bar", "Baz"}});

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new StructureDeleted(new StructureIdentifier("FooBar", 42)),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.False(item.Created, "item.Created");
            Assert.True(item.Deleted, "item.Deleted");
            Assert.Empty(item.Keys);
            Assert.Empty(item.Variables);
        }

        [Fact]
        public void ReplayHandlesDeletedVariables()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.Create(new Dictionary<string, string> {{"Foo", "Bar"}},
                        new Dictionary<string, string> {{"Bar", "Baz"}});

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new StructureVariablesModified(new StructureIdentifier("FooBar", 42),
                                                             new[] {ConfigKeyAction.Delete("Foo")}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.DoesNotContain("Foo", item.Variables.Keys);
        }

        [Fact]
        public void ReplayHandlesModifiedVariables()
        {
            var item = new ConfigStructure(new StructureIdentifier("FooBar", 42));

            item.Create(new Dictionary<string, string> {{"Foo", "Bar"}},
                        new Dictionary<string, string> {{"Bar", "Baz"}});

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new StructureVariablesModified(new StructureIdentifier("FooBar", 42),
                                                             new[] {ConfigKeyAction.Set("Foo", "BarBarBar")}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.Equal("BarBarBar", item.Variables["Foo"]);
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new ConfigStructure(new StructureIdentifier("FooBar", 42));
            snapshotSource.Create(new Dictionary<string, string> {{"Foo", "Bar"}},
                                  new Dictionary<string, string> {{"Bar", "Baz"}});
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new ConfigStructure(new StructureIdentifier("FooBar", 42));
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.Keys, target.Keys);
            Assert.Equal(snapshotSource.Variables, target.Variables);
            Assert.Equal(snapshotSource.Identifier, target.Identifier);
            Assert.Equal(snapshotSource.Created, target.Created);
            Assert.Equal(snapshotSource.Deleted, target.Deleted);
            Assert.Equal(snapshotSource.CurrentVersion, target.CurrentVersion);
            Assert.Equal(snapshotSource.MetaVersion, target.MetaVersion);
        }

        [Fact]
        public void ThrowsForNullIdentifier() => Assert.Throws<ArgumentNullException>(() => new ConfigStructure(null));
    }
}