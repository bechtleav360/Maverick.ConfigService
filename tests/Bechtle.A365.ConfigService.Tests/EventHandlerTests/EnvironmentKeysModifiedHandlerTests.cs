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
    public class EnvironmentKeysModifiedHandlerTests
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

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            dbMock.Setup(db => db.GenerateEnvironmentKeyAutocompleteData(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(envId, new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new EnvironmentKeysModifiedHandler(database, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task AutoCompleteSkippedWhenChangesNotApplied()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Error("environment not found", ErrorCode.NotFound));

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(new EnvironmentIdentifier("env-cat", "env-name"),
                                                  new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new EnvironmentKeysModifiedHandler(database, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleDomainEvent(domainEvent));
        }

        [Fact]
        public async Task ExceptionThrownWhenAutoCompleteFailed()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            dbMock.Setup(db => db.GenerateEnvironmentKeyAutocompleteData(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync(() => Result.Error("some error occured", ErrorCode.Undefined));

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(new EnvironmentIdentifier("env-cat", "env-name"),
                                                  new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new EnvironmentKeysModifiedHandler(database, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleDomainEvent(domainEvent));
        }

        [Fact]
        public async Task HandleDomainEventWithTargetAlreadyCreated()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            dbMock.Setup(db => db.GenerateEnvironmentKeyAutocompleteData(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var database = dbMock.Object;
            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(new EnvironmentIdentifier("env-cat", "env-name"),
                                                  new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;
            var handler = new EnvironmentKeysModifiedHandler(database, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            dbMock.Setup(db => db.GenerateEnvironmentKeyAutocompleteData(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(new EnvironmentIdentifier("env-foo", "env-bar"),
                                                  new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;

            var handler = new EnvironmentKeysModifiedHandler(dbMock.Object, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.ApplyChanges(It.IsAny<EnvironmentIdentifier>(), It.IsAny<IList<ConfigKeyAction>>()))
                  .ReturnsAsync(Result.Success);

            dbMock.Setup(db => db.GenerateEnvironmentKeyAutocompleteData(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync(Result.Success);

            var domainEvent = new Mock<EnvironmentKeysModified>(
                () => new EnvironmentKeysModified(new EnvironmentIdentifier("env-foo", "env-bar"),
                                                  new[] {ConfigKeyAction.Set("Foo", "Bar")})).Object;

            var handler = new EnvironmentKeysModifiedHandler(dbMock.Object, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.CreateEnvironment(It.IsAny<EnvironmentIdentifier>(), true))
                  .ReturnsAsync(Result.Success());

            var handler = new EnvironmentKeysModifiedHandler(dbMock.Object, new Mock<ILogger<EnvironmentKeysModifiedHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new EnvironmentKeysModifiedHandler(
                                  new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                  new Mock<ILogger<EnvironmentKeysModifiedHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new EnvironmentKeysModifiedHandler(null, null));
    }
}