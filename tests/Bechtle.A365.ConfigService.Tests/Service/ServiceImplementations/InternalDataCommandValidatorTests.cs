using System;
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
        private InternalDataCommandValidator CreateValidator() => new InternalDataCommandValidator();

        public static IEnumerable<object[]> InvalidEventIdentifiers => new[]
        {
            new object[] {null},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier())},
            new object[] {new ConfigurationBuilt(null, null, null)},
            new object[]
            {
                new ConfigurationBuilt(new ConfigurationIdentifier(), null, null)
            },
            new object[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(new EnvironmentIdentifier(),
                                                new StructureIdentifier(),
                                                0), null, null)
            },
            new object[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(new EnvironmentIdentifier(),
                                                new StructureIdentifier("Foo", 42),
                                                0), null, null)
            },
            new object[]
            {
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                new StructureIdentifier(),
                                                0), null, null)
            },
            new object[] {new EnvironmentCreated(null)},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier())},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier("Foo", string.Empty))},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier(string.Empty, "Bar"))},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier("Foo", null))},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier(null, "Bar"))},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier(string.Empty, string.Empty))},
            new object[] {new EnvironmentCreated(new EnvironmentIdentifier(null, null))},
            new object[] {new EnvironmentDeleted(null)},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier())},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier("Foo", string.Empty))},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier(string.Empty, "Bar"))},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier("Foo", null))},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier(null, "Bar"))},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier(string.Empty, string.Empty))},
            new object[] {new EnvironmentDeleted(new EnvironmentIdentifier(null, null))},
            new object[] {new DefaultEnvironmentCreated(null)},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier())},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", string.Empty))},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier(string.Empty, "Bar"))},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", null))},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier(null, "Bar"))},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier(string.Empty, string.Empty))},
            new object[] {new DefaultEnvironmentCreated(new EnvironmentIdentifier(null, null))},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier(), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", string.Empty), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier(string.Empty, "Bar"), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", null), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier(null, "Bar"), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier(string.Empty, string.Empty), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysImported(new EnvironmentIdentifier(null, null), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier(), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", string.Empty), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier(string.Empty, "Bar"), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", null), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier(null, "Bar"), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier(string.Empty, string.Empty), new ConfigKeyAction[0])},
            new object[] {new EnvironmentKeysModified(new EnvironmentIdentifier(null, null), new ConfigKeyAction[0])},
            new object[] {new StructureCreated(null, new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureCreated(new StructureIdentifier(), new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureCreated(new StructureIdentifier("Foo", 0), new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureCreated(new StructureIdentifier(null, 0), new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureCreated(new StructureIdentifier(string.Empty, 42), new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureCreated(new StructureIdentifier(string.Empty, 0), new Dictionary<string, string>(), new Dictionary<string, string>())},
            new object[] {new StructureDeleted(null)},
            new object[] {new StructureDeleted(new StructureIdentifier())},
            new object[] {new StructureDeleted(new StructureIdentifier("Foo", 0))},
            new object[] {new StructureDeleted(new StructureIdentifier(null, 0))},
            new object[] {new StructureDeleted(new StructureIdentifier(string.Empty, 42))},
            new object[] {new StructureDeleted(new StructureIdentifier(string.Empty, 0))},
            new object[] {new StructureVariablesModified(null, new ConfigKeyAction[0])},
            new object[] {new StructureVariablesModified(new StructureIdentifier(), new ConfigKeyAction[0])},
            new object[] {new StructureVariablesModified(new StructureIdentifier("Foo", 0), new ConfigKeyAction[0])},
            new object[] {new StructureVariablesModified(new StructureIdentifier(null, 0), new ConfigKeyAction[0])},
            new object[] {new StructureVariablesModified(new StructureIdentifier(string.Empty, 42), new ConfigKeyAction[0])},
            new object[] {new StructureVariablesModified(new StructureIdentifier(string.Empty, 0), new ConfigKeyAction[0])}
        };

        public static IEnumerable<object[]> InvalidConfigKeyActions => new[]
        {
            new object[] {null},
            new object[] {ConfigKeyAction.Set(null, null)},
            new object[] {ConfigKeyAction.Set(string.Empty, string.Empty)},
            new object[] {ConfigKeyAction.Set(string.Empty, null)},
            new object[] {ConfigKeyAction.Set(null, string.Empty)},
            new object[] {ConfigKeyAction.Set(string.Empty, "Bar")},
            new object[] {ConfigKeyAction.Set(null, "Bar")},
            new object[] {new ConfigKeyAction((ConfigKeyActionType) 42, "Foo", "Bar", "Description", "Value")}
        };

        [Theory]
        [MemberData(nameof(InvalidEventIdentifiers))]
        public void ValidateEventIdentifiers(object domainEvent)
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(domainEvent as DomainEvent);

            Assert.True(result.IsError);
        }

        /// <summary>
        ///     used by <see cref="InternalDataCommandValidatorTests.ValidateUnknownEventType" />
        /// </summary>
        private class UnknownDomainEvent : DomainEvent
        {
            /// <inheritdoc />
            public override bool Equals(DomainEvent other, bool strict) => throw new NotImplementedException();

            /// <inheritdoc />
            public override DomainEventMetadata GetMetadata() => throw new NotImplementedException();
        }

        [Theory]
        [MemberData(nameof(InvalidConfigKeyActions))]
        public void ValidateConfigKeyActions(ConfigKeyAction action)
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[] {action}));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateConfigurationBuilt()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new ConfigurationBuilt(
                    new ConfigurationIdentifier(
                        new EnvironmentIdentifier("Foo", "Bar"),
                        new StructureIdentifier("Foo", 42),
                        4711), null, null));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateDefaultEnvironmentCreated()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEmptyEventType()
        {
            var validator = CreateValidator();

            var eventMock = new Mock<DomainEvent>();
            eventMock.Setup(e => e.EventType)
                     .Returns("");

            var result = validator.ValidateDomainEvent(eventMock.Object);

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateEmptyListOfActions()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new EnvironmentKeysModified(
                    new EnvironmentIdentifier("Foo", "Bar"),
                    new ConfigKeyAction[0]));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentCreated()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentDeleted()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new EnvironmentDeleted(new EnvironmentIdentifier("Foo", "Bar")));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentKeysImported()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new EnvironmentKeysImported(new EnvironmentIdentifier("Foo", "Bar"), new[]
            {
                ConfigKeyAction.Set("Foo", "Bar", "description", "type")
            }));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateEnvironmentKeysModified()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[]
            {
                ConfigKeyAction.Set("Foo", "Bar", "description", "type"),
                ConfigKeyAction.Delete("Baz")
            }));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreated()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string> {{"Foo", "Bar"}},
                    new Dictionary<string, string>()));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedKeys_KeyEmpty()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string> {{string.Empty, "Bar"},},
                    new Dictionary<string, string>()));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedKeys_KeyNull()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string> {{"Foo", null}},
                    new Dictionary<string, string>()));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureCreatedVariables()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new StructureVariablesModified(
                    new StructureIdentifier("Foo", 42),
                    new[]
                    {
                        ConfigKeyAction.Set(string.Empty, string.Empty),
                        ConfigKeyAction.Set(null, null)
                    }));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureDeleted()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new StructureDeleted(new StructureIdentifier("Foo", 42)));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateStructureModifiedVariables()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(
                new StructureCreated(
                    new StructureIdentifier("Foo", 42),
                    new Dictionary<string, string> {{"Foo", "Bar"}},
                    new Dictionary<string, string>
                    {
                        {string.Empty, "Bar"},
                        {"Foo", null}
                    }));

            Assert.True(result.IsError);
        }

        [Fact]
        public void ValidateStructureVariablesModified()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new StructureVariablesModified(new StructureIdentifier("Foo", 42), new[]
            {
                ConfigKeyAction.Set("Foo", "Bar"),
                ConfigKeyAction.Delete("Baz")
            }));

            Assert.False(result.IsError);
        }

        [Fact]
        public void ValidateUnknownEventType()
        {
            var validator = CreateValidator();

            var result = validator.ValidateDomainEvent(new UnknownDomainEvent());

            Assert.True(result.IsError);
        }
    }
}