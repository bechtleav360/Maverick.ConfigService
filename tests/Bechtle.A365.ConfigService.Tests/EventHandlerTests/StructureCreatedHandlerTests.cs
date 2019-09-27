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
    public class StructureCreatedHandlerTests
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

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureCreated>(() => new StructureCreated(envId,
                                                                                    new Dictionary<string, string>(),
                                                                                    new Dictionary<string, string>())).Object;
            var handler = new StructureCreatedHandler(database, new Mock<ILogger<StructureCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetNotCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureCreated>(() => new StructureCreated(new StructureIdentifier("env-cat", 42),
                                                                                    new Dictionary<string, string>(),
                                                                                    new Dictionary<string, string>())).Object;
            var handler = new StructureCreatedHandler(database, new Mock<ILogger<StructureCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetAlreadyCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(() => Result.Error("structure already created", ErrorCode.StructureAlreadyExists));

            var database = dbMock.Object;
            var domainEvent = new Mock<StructureCreated>(() => new StructureCreated(new StructureIdentifier("env-cat", 42),
                                                                                    new Dictionary<string, string>(),
                                                                                    new Dictionary<string, string>())).Object;
            var handler = new StructureCreatedHandler(database, new Mock<ILogger<StructureCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureCreated>(
                () => new StructureCreated(new StructureIdentifier("env-foo", 42),
                                           new Dictionary<string, string>(),
                                           new Dictionary<string, string>())).Object;

            var handler = new StructureCreatedHandler(dbMock.Object, new Mock<ILogger<StructureCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateStructure(It.IsAny<StructureIdentifier>(),
                                                  It.IsAny<Dictionary<string, string>>(),
                                                  It.IsAny<Dictionary<string, string>>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<StructureCreated>(
                () => new StructureCreated(new StructureIdentifier("env-foo", 42),
                                           new Dictionary<string, string>
                                           {
                                               {"A", "{{Foo/*}}"},
                                               {"B", "{{Lorem/*}}"},
                                               {"C", "CVal"}
                                           },
                                           new Dictionary<string, string>
                                           {
                                               {"VarFoo", "True"},
                                               {"VarBar", "False"}
                                           })).Object;

            var handler = new StructureCreatedHandler(dbMock.Object, new Mock<ILogger<StructureCreatedHandler>>().Object);

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

            var handler = new StructureCreatedHandler(dbMock.Object, new Mock<ILogger<StructureCreatedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new StructureCreatedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<StructureCreatedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new StructureCreatedHandler(null, null));
    }
}