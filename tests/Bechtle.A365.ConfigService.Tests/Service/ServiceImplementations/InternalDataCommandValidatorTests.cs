﻿using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Implementations;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations
{
    public class InternalDataCommandValidatorTests
    {
        // invalid key-actions might be inserted through deserialization
#pragma warning disable 8625
        public static IEnumerable<object?[]> InvalidConfigKeyActions => new[]
        {
            new object?[] { null },
            new object?[] { ConfigKeyAction.Set(null, null) },
            new object?[] { ConfigKeyAction.Set(string.Empty, string.Empty) },
            new object?[] { ConfigKeyAction.Set(string.Empty, null) },
            new object?[] { ConfigKeyAction.Set(null, string.Empty) },
            new object?[] { ConfigKeyAction.Set(string.Empty, "Bar") },
            new object?[] { ConfigKeyAction.Set(null, "Bar") },
            new object?[] { new ConfigKeyAction((ConfigKeyActionType)42, "Foo", "Bar", "Description", "Value") }
        };
#pragma warning restore 8625

        // invalid key-actions might be inserted through deserialization
#pragma warning disable 8625
        public static IEnumerable<object?[]> InvalidEventIdentifiers => new[]
        {
            new object?[] { null },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier()) },
            new object?[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier(),
                        new StructureIdentifier(),
                        0),
                    null,
                    null)
            },
            new object?[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier(),
                        new StructureIdentifier("Foo", 42),
                        0),
                    null,
                    null)
            },
            new object?[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier("Foo", "Bar"),
                        new StructureIdentifier(),
                        0),
                    null,
                    null)
            },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier()) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier("Foo", string.Empty)) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier(string.Empty, "Bar")) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier("Foo", null)) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier(null, "Bar")) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier(string.Empty, string.Empty)) },
            new object?[] { new EnvironmentCreated(new EnvironmentIdentifier(null, null)) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier()) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier("Foo", string.Empty)) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier(string.Empty, "Bar")) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier("Foo", null)) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier(null, "Bar")) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier(string.Empty, string.Empty)) },
            new object?[] { new EnvironmentDeleted(new EnvironmentIdentifier(null, null)) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier()) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", string.Empty)) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier(string.Empty, "Bar")) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", null)) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier(null, "Bar")) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier(string.Empty, string.Empty)) },
            new object?[] { new DefaultEnvironmentCreated(new EnvironmentIdentifier(null, null)) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier(), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", string.Empty), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier(string.Empty, "Bar"), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", null), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier(null, "Bar"), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier(string.Empty, string.Empty), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayersModified(new EnvironmentIdentifier(null, null), Array.Empty<LayerIdentifier>()) },
            new object?[] { new EnvironmentLayerKeysImported(new LayerIdentifier(), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysImported(new LayerIdentifier(string.Empty), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysImported(new LayerIdentifier("Bar"), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysImported(new LayerIdentifier(null), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysModified(new LayerIdentifier(), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysModified(new LayerIdentifier(string.Empty), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysModified(new LayerIdentifier("Bar"), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerKeysModified(new LayerIdentifier(null), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new EnvironmentLayerCreated(new LayerIdentifier()) },
            new object?[] { new EnvironmentLayerCreated(new LayerIdentifier(string.Empty)) },
            new object?[] { new EnvironmentLayerCreated(new LayerIdentifier(null)) },
            new object?[] { new EnvironmentLayerDeleted(new LayerIdentifier()) },
            new object?[] { new EnvironmentLayerDeleted(new LayerIdentifier(string.Empty)) },
            new object?[] { new EnvironmentLayerDeleted(new LayerIdentifier(null)) },
            new object?[] { new StructureCreated(new StructureIdentifier(), new Dictionary<string, string?>(), new Dictionary<string, string?>()) },
            new object?[] { new StructureCreated(new StructureIdentifier("Foo", 0), new Dictionary<string, string?>(), new Dictionary<string, string?>()) },
            new object?[] { new StructureCreated(new StructureIdentifier(null, 0), new Dictionary<string, string?>(), new Dictionary<string, string?>()) },
            new object?[]
            {
                new StructureCreated(new StructureIdentifier(string.Empty, 42), new Dictionary<string, string?>(), new Dictionary<string, string?>())
            },
            new object?[]
            {
                new StructureCreated(new StructureIdentifier(string.Empty, 0), new Dictionary<string, string?>(), new Dictionary<string, string?>())
            },
            new object?[] { new StructureDeleted(new StructureIdentifier()) },
            new object?[] { new StructureDeleted(new StructureIdentifier("Foo", 0)) },
            new object?[] { new StructureDeleted(new StructureIdentifier(null, 0)) },
            new object?[] { new StructureDeleted(new StructureIdentifier(string.Empty, 42)) },
            new object?[] { new StructureDeleted(new StructureIdentifier(string.Empty, 0)) },
            new object?[] { new StructureVariablesModified(new StructureIdentifier(), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new StructureVariablesModified(new StructureIdentifier("Foo", 0), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new StructureVariablesModified(new StructureIdentifier(null, 0), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new StructureVariablesModified(new StructureIdentifier(string.Empty, 42), Array.Empty<ConfigKeyAction>()) },
            new object?[] { new StructureVariablesModified(new StructureIdentifier(string.Empty, 0), Array.Empty<ConfigKeyAction>()) }
        };
#pragma warning restore 8625

        [Theory]
        [MemberData(nameof(InvalidConfigKeyActions))]
        public void ValidateConfigKeyActions(ConfigKeyAction action)
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[] { action }));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateConfigurationBuilt()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier("Foo", "Bar"),
                        new StructureIdentifier("Foo", 42),
                        4711),
                    null,
                    null));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateDefaultEnvironmentCreated()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEmptyEventType()
        {
            InternalDataCommandValidator validator = CreateValidator();

            var eventMock = new Mock<DomainEvent>();
            eventMock.Setup(e => e.EventType)
                     .Returns("");

            IResult result = validator.ValidateDomainEvent(eventMock.Object);

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateEmptyListOfActions()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new EnvironmentLayerKeysModified(
                    new LayerIdentifier("Foo"),
                    Array.Empty<ConfigKeyAction>()));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentCreated()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentDeleted()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new EnvironmentDeleted(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentKeysImported()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new EnvironmentLayerKeysImported(
                    new LayerIdentifier("Foo"),
                    new[]
                    {
                        ConfigKeyAction.Set("Foo", "Bar", "description", "type")
                    }));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentKeysModified()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new EnvironmentLayerKeysModified(
                    new LayerIdentifier("Foo"),
                    new[]
                    {
                        ConfigKeyAction.Set("Foo", "Bar", "description", "type"),
                        ConfigKeyAction.Delete("Baz")
                    }));

            Assert.False(result.IsError);
        }

        [Theory]
        [MemberData(nameof(InvalidEventIdentifiers))]
        public void ValidateEventIdentifiers(object domainEvent)
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(domainEvent as DomainEvent);

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreated()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string?> { { "Foo", "Bar" } },
                    new Dictionary<string, string?>()));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedKeys_KeyEmpty()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string?> { { string.Empty, "Bar" } },
                    new Dictionary<string, string?>()));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedKeys_KeyNull()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string?> { { "Foo", null } },
                    new Dictionary<string, string?>()));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedVariables()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureVariablesModified(
                    new StructureIdentifier("Foo", 42),
                    new[]
                    {
                        ConfigKeyAction.Set(string.Empty, string.Empty),
                        ConfigKeyAction.Set(null!, null)
                    }));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureDeleted()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new StructureDeleted(new StructureIdentifier("Foo", 42)));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureModifiedVariables()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string?> { { "Foo", "Bar" } },
                    new Dictionary<string, string?>
                    {
                        { string.Empty, "Bar" },
                        { "Foo", null }
                    }));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureVariablesModified()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(
                new StructureVariablesModified(
                    new StructureIdentifier("Foo", 42),
                    new[]
                    {
                        ConfigKeyAction.Set("Foo", "Bar"),
                        ConfigKeyAction.Delete("Baz")
                    }));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateUnknownEventType()
        {
            InternalDataCommandValidator validator = CreateValidator();

            IResult result = validator.ValidateDomainEvent(new UnknownDomainEvent());

            Assert.True(result.IsError);
        }

        private InternalDataCommandValidator CreateValidator() => new();

        /// <summary>
        ///     used by <see cref="InternalDataCommandValidatorTests.ValidateUnknownEventType" />
        /// </summary>
        private class UnknownDomainEvent : DomainEvent
        {
            /// <inheritdoc />
            public override DomainEventMetadata GetMetadata() => throw new NotImplementedException();
        }
    }
}
