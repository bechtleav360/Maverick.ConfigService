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
    public class EnvironmentCreatedHandlerTests
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

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), false))
                  .ReturnsAsync(Result.Success());

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentCreated>(() => new EnvironmentCreated(envId)).Object;
            var handler = new EnvironmentCreatedHandler(database, new Mock<ILogger<EnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleDomainEventWithTargetNotCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), false))
                  .ReturnsAsync(Result.Success());

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentCreated>(() => new EnvironmentCreated(new EnvironmentIdentifier("env-cat", "env-name"))).Object;
            var handler = new EnvironmentCreatedHandler(database, new Mock<ILogger<EnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), false))
                  .ReturnsAsync(Result.Success());

            var domainEvent = new Mock<EnvironmentCreated>(
                () => new EnvironmentCreated(new EnvironmentIdentifier("env-foo", "env-bar"))).Object;

            var handler = new EnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<EnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), false))
                  .ReturnsAsync(Result.Success());

            var domainEvent = new Mock<EnvironmentCreated>(
                () => new EnvironmentCreated(new EnvironmentIdentifier("env-foo", "env-bar"))).Object;

            var handler = new EnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<EnvironmentCreatedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect())
                  .ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), false))
                  .ReturnsAsync(Result.Success());

            var handler = new EnvironmentCreatedHandler(dbMock.Object, new Mock<ILogger<EnvironmentCreatedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new EnvironmentCreatedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<EnvironmentCreatedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new EnvironmentCreatedHandler(null, null));
    }
}