using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.EventHandlerTests
{
    public class StructureVariablesModifiedHandlerTests
    {
        /// <summary>
        ///     test-data for <see cref="HandleDomainEventWithIncorrectTarget" />
        /// </summary>
        public static IEnumerable<object[]> IncorrectTargetData => new[]
        {
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier("", 0)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier("", 0)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier("", 0)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier("", 0)},
            new object[] {new StructureIdentifier(null, -1)},
            new object[] {new StructureIdentifier("", 0)}
        };

        [Theory]
        [MemberData(nameof(IncorrectTargetData))]
        public async Task HandleDomainEventWithIncorrectTarget(StructureIdentifier envId)
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<StructureIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureVariablesModified>(
                () => new StructureVariablesModified(envId, new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new StructureVariablesModifiedHandler(database, new Mock<ILogger<StructureVariablesModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetNotCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<StructureIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureVariablesModified>(
                () => new StructureVariablesModified(new StructureIdentifier("env-cat", 42),
                                                     new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new StructureVariablesModifiedHandler(database, new Mock<ILogger<StructureVariablesModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<StructureIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureVariablesModified>(
                () => new StructureVariablesModified(new StructureIdentifier("env-foo", 42),
                                                     new ConfigKeyAction[0])).Object;

            var handler = new StructureVariablesModifiedHandler(dbMock.Object, new Mock<ILogger<StructureVariablesModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<StructureIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureVariablesModified>(
                () => new StructureVariablesModified(new StructureIdentifier("env-foo", 42),
                                                     new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;

            var handler = new StructureVariablesModifiedHandler(dbMock.Object, new Mock<ILogger<StructureVariablesModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(Result.Success);

            var handler = new StructureVariablesModifiedHandler(dbMock.Object, new Mock<ILogger<StructureVariablesModifiedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new StructureVariablesModifiedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<StructureVariablesModifiedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new StructureVariablesModifiedHandler(null, null));
    }
}