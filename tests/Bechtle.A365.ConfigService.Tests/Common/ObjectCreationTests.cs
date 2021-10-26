using System;
using System.Collections.Generic;
using System.Text.Json;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    /// <summary>
    ///     These tests act as sort of a sanity-test to prevent
    ///     changes to these objects, that would prevent their creation.
    /// </summary>
    public class ObjectCreationTests
    {
        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigEnvironment() => new ConfigEnvironment
        {
            Id = new EnvironmentIdentifier("Foo", "Bar"),
            Json = "{}",
            Keys = new Dictionary<string, EnvironmentLayerKey> { { "Foo", new EnvironmentLayerKey() } },
            Layers = new List<LayerIdentifier> { new("Foo") },
            ChangedAt = DateTime.Now,
            ChangedBy = "Me",
            CreatedAt = DateTime.Now,
            CreatedBy = "Me",
            CurrentVersion = 4711,
            IsDefault = true,
            KeyPaths = new List<EnvironmentLayerKeyPath> { new("Foo") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigExport() => new ConfigExport
        {
            Environments = new[]
            {
                new EnvironmentExport
                {
                    Name = "Foo",
                    Category = "Bar",
                    Layers = new[] { new LayerIdentifier("Foo") }
                }
            },
            Layers = new[]
            {
                new LayerExport
                {
                    Name = "Foo", Keys = new[]
                    {
                        new EnvironmentKeyExport
                        {
                            Key = "Foo",
                            Value = "Bar",
                            Type = "String",
                            Description = "Exported Key"
                        }
                    }
                }
            }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigStructure() => new ConfigStructure
        {
            Id = new StructureIdentifier("Foo", 4711),
            Keys = new Dictionary<string, string?> { { "Foo", "Bar" } },
            Variables = new Dictionary<string, string?> { { "Foo", "Bar" } },
            ChangedAt = DateTime.Now,
            ChangedBy = "Me",
            CreatedAt = DateTime.Now,
            CreatedBy = "Me",
            CurrentVersion = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigurationBuildOptions() => new ConfigurationBuildOptions
        {
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today + TimeSpan.FromDays(1)
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigurationBuilt() => new ConfigurationBuilt
        {
            Identifier = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 4711),
                4711),
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today + TimeSpan.FromDays(1)
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ConfigurationIdentifier() => new ConfigurationIdentifier
        {
            Environment = new EnvironmentIdentifier("Foo", "Bar"),
            Structure = new StructureIdentifier("Foo", 4711),
            Version = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_DefaultEnvironmentCreated() => new DefaultEnvironmentCreated
        {
            Identifier = new EnvironmentIdentifier("Foo", "Bar")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_DtoConfigKey() => new DtoConfigKey
        {
            Key = "Foo",
            Value = "Bar",
            Type = "String",
            Description = "Some Description"
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_DtoConfigKeyCompletion() => new DtoConfigKeyCompletion
        {
            Completion = "Foo/Bar/Baz",
            FullPath = "Foo/Bar",
            HasChildren = true
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_DtoStructure() => new DtoStructure
        {
            Name = "Foo",
            Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
            Variables = new Dictionary<string, object> { { "foo", "bar" } },
            Version = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentCreated() => new EnvironmentCreated
        {
            Identifier = new EnvironmentIdentifier("Foo", "Bar")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentDeleted() => new EnvironmentDeleted
        {
            Identifier = new EnvironmentIdentifier("Foo", "Bar")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentExport() => new EnvironmentExport
        {
            Name = "Foo",
            Category = "Bar",
            Layers = new[] { new LayerIdentifier("Foo") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentIdentifier() => new EnvironmentIdentifier
        {
            Category = "Foo",
            Name = "Bar"
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentKeyExport() => new EnvironmentKeyExport
        {
            Key = "Foo",
            Value = "Bar",
            Type = "String",
            Description = "Exported Key"
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayer() => new EnvironmentLayer
        {
            Id = new LayerIdentifier("Foo"),
            Json = "{}",
            Keys = new Dictionary<string, EnvironmentLayerKey> { { "Foo", new EnvironmentLayerKey("Foo", "Bar", string.Empty, string.Empty, 4711) } },
            Tags = new List<string> { "Foo", "Bar" },
            KeyPaths = new List<EnvironmentLayerKeyPath> { new("Foo") },
            ChangedAt = DateTime.Now,
            ChangedBy = "Me",
            CreatedAt = DateTime.Now,
            CreatedBy = "Me",
            CurrentVersion = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerCopied() => new EnvironmentLayerCopied
        {
            SourceIdentifier = new LayerIdentifier("Foo"),
            TargetIdentifier = new LayerIdentifier("Bar")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerCreated() => new EnvironmentLayerCreated
        {
            Identifier = new LayerIdentifier("Foo")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerDeleted() => new EnvironmentLayerDeleted
        {
            Identifier = new LayerIdentifier("Foo")
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerKey() => new EnvironmentLayerKey
        {
            Key = "Foo",
            Value = "Bar",
            Type = "String",
            Description = "This is a Test",
            Version = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerKeyPath() => new EnvironmentLayerKeyPath
        {
            Path = "Bar",
            ParentPath = "Foo",
            Children = { new EnvironmentLayerKeyPath("Baz") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerKeysImported() => new EnvironmentLayerKeysImported
        {
            Identifier = new LayerIdentifier("Foo"),
            ModifiedKeys = new[] { new ConfigKeyAction(ConfigKeyActionType.Set, "Foo", "Bar", "Description", "Type") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayerKeysModified() => new EnvironmentLayerKeysModified
        {
            Identifier = new LayerIdentifier("Foo"),
            ModifiedKeys = new[] { new ConfigKeyAction(ConfigKeyActionType.Set, "Foo", "Bar", "Description", "Type") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_EnvironmentLayersModified() => new EnvironmentLayersModified
        {
            Identifier = new EnvironmentIdentifier("Foo", "Bar"),
            Layers = new List<LayerIdentifier> { new("Foo") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_ExportDefinition() => new ExportDefinition
        {
            Environments = new[] { new EnvironmentIdentifier("Foo", "Bar") },
            Layers = new[] { new LayerIdentifier("Foo") }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_LayerIdentifier() => new LayerIdentifier
        {
            Name = "Foo"
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_PreparedConfiguration() => new PreparedConfiguration
        {
            Id = new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"), new StructureIdentifier("Foo", 4711), 4711),
            Json = "{}",
            Keys = new Dictionary<string, string?> { { "Foo", "Bar" } },
            ChangedAt = DateTime.Now,
            ChangedBy = "Me",
            CreatedAt = DateTime.Now,
            CreatedBy = "Me",
            CurrentVersion = 4711,
            Errors = new Dictionary<string, List<string>> { { "Foo", new List<string> { "Error1" } } },
            Warnings = new Dictionary<string, List<string>> { { "Foo", new List<string> { "Warning1" } } },
            UsedKeys = new List<string> { "Foo" },
            ConfigurationVersion = 4711,
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today + TimeSpan.FromDays(1)
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_StructureCreated() => new StructureCreated
        {
            Identifier = new StructureIdentifier("Foo", 4711),
            Keys = new Dictionary<string, string?> { { "Foo", "Bar" } },
            Variables = new Dictionary<string, string?> { { "Foo", "Bar" } }
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_StructureDeleted() => new StructureDeleted
        {
            Identifier = new StructureIdentifier("Foo", 4711)
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_StructureIdentifier() => new StructureIdentifier
        {
            Name = "Foo",
            Version = 4711
        };

        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void IsConstructable_StructureVariablesModified() => new StructureVariablesModified
        {
            Identifier = new StructureIdentifier("Foo", 4711),
            ModifiedKeys = new[] { new ConfigKeyAction(ConfigKeyActionType.Set, "Foo", "Bar", "Description", "Type") }
        };
    }
}
