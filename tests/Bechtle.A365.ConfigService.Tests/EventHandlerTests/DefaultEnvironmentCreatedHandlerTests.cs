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
    public class DefaultEnvironmentCreatedHandlerTests
    {
        /// <summary>
        ///     test-data for <see cref="HandleDomainEventWithIncorrectTarget" />
        /// </summary>
        public static IEnumerable<object[]> IncorrectTargetData => new[]
        {
            new object[] {new EnvironmentIdentifier(null, null)},
            new object[] {new EnvironmentIdentifier("", "")},
            new object[] {new EnvironmentIdentifier(null, null)},
            new object[] {new EnvironmentIdentifier("", "")},
            new object[] {new EnvironmentIdentifier(null, null)},
            new object[] {new EnvironmentIdentifier("", "")},
            new object[] {new EnvironmentIdentifier(null, null)},
            new object[] {new EnvironmentIdentifier("", "")},
            new object[] {new EnvironmentIdentifier(null, null)},
            new object[] {new EnvironmentIdentifier("", "")}
        };

        [Theory]
        [MemberData(nameof(IncorrectTargetData))]
        public async Task HandleDomainEventWithIncorrectTarget(EnvironmentIdentifier envId)
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var database = dbMock.Object;
            var domainEvent = new Mock<DefaultEnvironmentCreated>(() => new DefaultEnvironmentCreated(envId)).Object;
            var handler = new DefaultEnvironmentCreatedHandler(database, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetNotCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var database = dbMock.Object;
            var domainEvent = new Mock<DefaultEnvironmentCreated>(() => new DefaultEnvironmentCreated(new EnvironmentIdentifier("env-cat", "env-name"))).Object;
            var handler = new DefaultEnvironmentCreatedHandler(database, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetAlreadyCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Error("environment already created", ErrorCode.EnvironmentAlreadyExists));

            var database = dbMock.Object;
            var domainEvent = new Mock<DefaultEnvironmentCreated>(() => new DefaultEnvironmentCreated(new EnvironmentIdentifier("env-cat", "env-name"))).Object;
            var handler = new DefaultEnvironmentCreatedHandler(database, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var domainEvent = new Mock<DefaultEnvironmentCreated>(
                () => new DefaultEnvironmentCreated(new EnvironmentIdentifier("env-foo", "env-bar"))).Object;

            var handler = new DefaultEnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var domainEvent = new Mock<DefaultEnvironmentCreated>(
                () => new DefaultEnvironmentCreated(new EnvironmentIdentifier("env-foo", "env-bar"))).Object;

            var handler = new DefaultEnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var handler = new DefaultEnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<DefaultEnvironmentCreatedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new DefaultEnvironmentCreatedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<DefaultEnvironmentCreatedHandler>>(MockBehavior.Strict).Object));

        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new DefaultEnvironmentCreatedHandler(null, null));
    }
}