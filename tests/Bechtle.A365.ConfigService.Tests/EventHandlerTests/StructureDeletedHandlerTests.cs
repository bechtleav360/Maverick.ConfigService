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
    public class StructureDeletedHandlerTests
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

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureDeleted>(() => new StructureDeleted(envId)).Object;
            var handler = new StructureDeletedHandler(database, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetNotCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureDeleted>(() => new StructureDeleted(new StructureIdentifier("env-cat", 42))).Object;
            var handler = new StructureDeletedHandler(database, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetAlreadyCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(() => Result.Error("structure already created", ErrorCode.StructureAlreadyExists));

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureDeleted>(() => new StructureDeleted(new StructureIdentifier("env-cat", 42))).Object;
            var handler = new StructureDeletedHandler(database, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureDeleted>(
                () => new StructureDeleted(new StructureIdentifier("env-foo", 42))).Object;

            var handler = new StructureDeletedHandler(dbMock.Object, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureDeleted>(
                () => new StructureDeleted(new StructureIdentifier("env-foo", 42))).Object;

            var handler = new StructureDeletedHandler(dbMock.Object, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.DeleteStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var handler = new StructureDeletedHandler(dbMock.Object, new Mock<ILogger<StructureDeletedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new StructureDeletedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<StructureDeletedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new StructureDeletedHandler(null, null));
    }
}