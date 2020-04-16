using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
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
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
        }

        [Fact]
        public void CreateAssignsValues()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            Assert.True(item.Created);
            Assert.Empty(item.Keys);
        }

        [Fact]
        public void CreateDefault()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create(true);

            Assert.True(item.Created);
            Assert.False(item.Deleted);
            Assert.True(item.IsDefault);
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
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
                DomainEvent = new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", "Bar"),
                                                          new[] {ConfigKeyAction.Set("Jar", "Jar", "Jar", "Jar")}),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            item.Delete();

            Assert.True(item.Deleted, "item.Deleted");
            Assert.False(item.Created, "item.Created");
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
        }

        [Fact]
        public void DeleteKeysChangesValues()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Bar", "", "", 0)});

            Assert.NotEmpty(item.Keys);

            item.DeleteKeys(new List<string> {"Foo"});

            Assert.Empty(item.Keys);
        }

        [Fact]
        public void DeletingEmptyListFails()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            var result = item.DeleteKeys(new List<string>());

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void DeletingInvalidKeysFails()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Bar", "", "", 0)});

            Assert.NotEmpty(item.Keys);

            var result = item.DeleteKeys(new List<string> {"Baz"});

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void DeletingNullListFails()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            var result = item.DeleteKeys(null);

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void GenerateCorrectKeyPathsSimple()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ImportKeys(new[]
            {
                new ConfigEnvironmentKey("Foo", "Bar", "", "", 1),
                new ConfigEnvironmentKey("Jar/Jar", "Binks", "", "", 1),
                new ConfigEnvironmentKey("Guy/0001", "1", "", "", 1),
                new ConfigEnvironmentKey("Guy/0002", "2", "", "", 1),
                new ConfigEnvironmentKey("Guy/0003", "3", "", "", 1)
            });

            var paths = item.KeyPaths;

            Assert.Contains(paths, p => p.FullPath == "Foo");
            Assert.Contains(paths, p => p.FullPath == "Jar/");
            Assert.Contains(paths, p => p.FullPath == "Guy/");

            var jar = paths.First(p => p.FullPath == "Jar/");
            var guy = paths.First(p => p.FullPath == "Guy/");

            Assert.Contains(jar.Children, p => p.FullPath == "Jar/Jar");

            Assert.Contains(guy.Children, p => p.FullPath == "Guy/0001");
            Assert.Contains(guy.Children, p => p.FullPath == "Guy/0002");
            Assert.Contains(guy.Children, p => p.FullPath == "Guy/0003");
        }

        [Fact]
        public void GenerateCorrectKeyPathsNestedObject()
        {
            static T AssertAndGet<T>(ICollection<T> enumerable, Func<T, bool> predicate)
            {
                Assert.Contains(enumerable, i => predicate(i));
                return enumerable.First(predicate);
            }

            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ImportKeys(new[]
            {
                new ConfigEnvironmentKey("A/B/C", "1", "", "", 1),
                new ConfigEnvironmentKey("A/B/D/E", "2", "", "", 1),
                new ConfigEnvironmentKey("A/B/D/F", "3", "", "", 1),
            });

            var a = AssertAndGet(item.KeyPaths, p => p.Path == "A");
            var b = AssertAndGet(a?.Children, p => p.FullPath == "A/B/" && p.Path == "B");
            var c = AssertAndGet(b?.Children, p => p.FullPath == "A/B/C" && p.Path == "C");
            var d = AssertAndGet(b?.Children, p => p.FullPath == "A/B/D/" && p.Path == "D");
            var e = AssertAndGet(d?.Children, p => p.FullPath == "A/B/D/E" && p.Path == "E");
            var f = AssertAndGet(d?.Children, p => p.FullPath == "A/B/D/F" && p.Path == "F");
        }

        [Fact]
        public void GetKeysAsDictionary()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", "Bar"),
                                                          new[] {ConfigKeyAction.Set("Jar", "Jar", "Jar", "Jar")}),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            var dict = item.GetKeysAsDictionary();

            Assert.Single(dict);
            Assert.Equal("Jar", dict["Jar"]);
        }

        [Fact]
        public void ImportKeys()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.ImportKeys(new[]
            {
                new ConfigEnvironmentKey("Foo", "Bar", "Baz", "Que", 1),
                new ConfigEnvironmentKey("Jar", "Jar", "Jar", "Jar", 2)
            });

            Assert.True(item.Keys.Count == 2, "item.Keys.Count == 2");

            Assert.Equal("Bar", item.Keys["Foo"].Value);
            Assert.Equal("Baz", item.Keys["Foo"].Type);
            Assert.Equal("Que", item.Keys["Foo"].Description);

            Assert.Equal("Jar", item.Keys["Jar"].Value);
            Assert.Equal("Jar", item.Keys["Jar"].Type);
            Assert.Equal("Jar", item.Keys["Jar"].Description);
        }

        [Fact]
        public void ImportOverwritesExisting()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.UpdateKeys(new[] {new ConfigEnvironmentKey("Old", "Value", "Foo", "Bar", 42)});

            item.ImportKeys(new[]
            {
                new ConfigEnvironmentKey("Foo", "Bar", "Baz", "Que", 1),
                new ConfigEnvironmentKey("Jar", "Jar", "Jar", "Jar", 2)
            });

            Assert.True(item.Keys.Count == 2, "item.Keys.Count == 2");

            Assert.DoesNotContain("Old", item.Keys.Keys);
        }

        [Fact]
        public void InitializedProperties()
        {
            var identifier = new EnvironmentIdentifier("Foo", "Bar");

            var item = new ConfigEnvironment(identifier);

            Assert.NotNull(item.Keys);
            Assert.Empty(item.Keys);

            Assert.NotNull(item.KeyPaths);
            Assert.Empty(item.KeyPaths);

            Assert.False(item.Created);
            Assert.False(item.Deleted);
            Assert.False(item.IsDefault);

            // use this comparison because we don't care about reference-equality, only value-equality
            Assert.True(identifier.Equals(item.Identifier), "identifier.Equals(item.Identifier)");
        }

        [Fact]
        public void ModifyingEmptyListFails()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            var result = item.UpdateKeys(new List<ConfigEnvironmentKey>());

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void ModifyingNullListFails()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            var result = item.UpdateKeys(null);

            Assert.True(result.IsError, "result.IsError");
        }

        [Fact]
        public void ModifyVariablesChangesValues()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(item.Keys);

            item.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Bar", "", "", 0)});

            Assert.NotEmpty(item.Keys);
        }

        [Fact]
        public void OverwriteExistingVariables()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Bar", "", "", 0)});
            item.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Baz", "", "", 0)});

            Assert.Equal("Baz", item.Keys["Foo"].Value);
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
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
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
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
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
            Assert.Empty(item.Keys);
            Assert.Empty(item.KeyPaths);
        }

        [Fact]
        public void ReplayHandlesDeletedVariables()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[] {ConfigKeyAction.Delete("Foo")}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.DoesNotContain("Foo", item.Keys.Keys);
        }

        [Fact]
        public void ReplayHandlesImportedKeys()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", "Bar"), new[] {ConfigKeyAction.Set("Foo", "BarBarBar")}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.Single(item.Keys);
            Assert.Equal("BarBarBar", item.Keys["Foo"].Value);
        }

        [Fact]
        public void ReplayHandlesModifiedVariables()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();

            item.ApplyEvent(new ReplayedEvent
            {
                Version = 1,
                DomainEvent = new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[] {ConfigKeyAction.Set("Foo", "BarBarBar")}),
                UtcTime = DateTime.UtcNow
            });

            Assert.Equal(1, item.CurrentVersion);
            Assert.Equal("BarBarBar", item.Keys["Foo"].Value);
        }

        [Fact]
        public void SnapshotAppliesAllProperties()
        {
            var snapshotSource = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            snapshotSource.Create();
            snapshotSource.UpdateKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Foo", "Bar", "", "", 0)});
            var snapshot = snapshotSource.CreateSnapshot();

            var target = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            target.ApplySnapshot(snapshot);

            Assert.Equal(snapshotSource.Keys, target.Keys);
            Assert.Equal(snapshotSource.KeyPaths, target.KeyPaths);
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