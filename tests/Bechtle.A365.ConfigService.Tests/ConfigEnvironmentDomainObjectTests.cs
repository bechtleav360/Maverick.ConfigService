using System.Collections.Generic;
using System.Threading.Tasks;
using App.Metrics;
using AutoFixture.Xunit2;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class ConfigEnvironmentDomainObjectTests
    {
        public ConfigEnvironmentDomainObjectTests()
        {
            _metrics = AppMetrics.CreateDefaultBuilder().Build();
        }

        public static IEnumerable<object[]> IdentifiedByParameters() => new[]
        {
            new object[] {new EnvironmentIdentifier(string.Empty, string.Empty), false},
            new object[] {new EnvironmentIdentifier(string.Empty, string.Empty), true},
            new object[] {new EnvironmentIdentifier("Foo", "Bar"), false},
            new object[] {new EnvironmentIdentifier("Foo", "Bar"), true},
            new object[] {new EnvironmentIdentifier(null, null), false},
            new object[] {new EnvironmentIdentifier(null, null), true},
            new object[] {new EnvironmentIdentifier(null, string.Empty), true},
            new object[] {new EnvironmentIdentifier(null, string.Empty), false},
            new object[] {new EnvironmentIdentifier(string.Empty, null), true},
            new object[] {new EnvironmentIdentifier(string.Empty, null), false}
        };

        public static IEnumerable<object[]> SkippedEventStates => new[]
        {
            new object[] {EventStatus.Projected},
            new object[] {EventStatus.Recorded}
        };

        public static IEnumerable<object[]> ProcessedEventStates => new[]
        {
            new object[] {EventStatus.Unknown},
            new object[] {EventStatus.Superseded}
        };

        private readonly IMetrics _metrics;

        [Theory]
        [MemberData(nameof(IdentifiedByParameters))]
        public void IdentifiedBy(EnvironmentIdentifier identifier, bool isDefault)
            => new ConfigEnvironment().IdentifiedBy(identifier, isDefault);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("_")]
        [InlineData(":")]
        [InlineData(null)]
        [InlineData("Foo")]
        public void DefaultIdentifiedBy(string category)
            => new ConfigEnvironment().DefaultIdentifiedBy(category);

        [Theory]
        [AutoData]
        public void ImportKeys(IEnumerable<DtoConfigKey> keys) => new ConfigEnvironment().ImportKeys(keys);

        [Theory]
        [AutoData]
        public void ModifyKeys(IEnumerable<ConfigKeyAction> keys) => new ConfigEnvironment().ModifyKeys(keys);

        [Theory]
        [MemberData(nameof(SkippedEventStates))]
        public async Task SkipEvents(EventStatus status)
        {
            // nothing is setup, because nothing should be called when no events have been added
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var logger = new Mock<ILogger<ConfigEnvironmentDomainObjectTests>>(MockBehavior.Strict);

            // setup history to return the desired state - all events have been seen and shouldn't be saved again
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            history.Setup(h => h.GetEventStatus(It.IsAny<DomainEvent>()))
                   .ReturnsAsync(status);

            var domainObject = new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier("Foo", "Bar"))
                                                      .Create();

            await domainObject.Save(store.Object,
                                    history.Object,
                                    logger.Object,
                                    _metrics);
        }

        [Theory]
        [MemberData(nameof(SkippedEventStates))]
        public async Task ProcessEvents(EventStatus status)
        {
            var logger = new Mock<ILogger<ConfigEnvironmentDomainObjectTests>>(MockBehavior.Strict);

            // setup store to appear as if everything was written correctly
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            store.Setup(s => s.WriteEvent(It.IsAny<DomainEvent>()))
                 .Returns(Task.CompletedTask);

            // setup history to return the desired state - all events are new and should be saved
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            history.Setup(h => h.GetEventStatus(It.IsAny<DomainEvent>()))
                   .ReturnsAsync(status);

            var domainObject = new ConfigEnvironment().IdentifiedBy(new EnvironmentIdentifier("Foo", "Bar"))
                                                      .Create();

            await domainObject.Save(store.Object,
                                    history.Object,
                                    logger.Object,
                                    _metrics);

            history.Verify(s => s.GetEventStatus(It.IsAny<EnvironmentCreated>()), Times.Once);
            store.Verify(s=>s.WriteEvent(It.IsAny<EnvironmentCreated>()), Times.Once);
        }

        [Fact]
        public void Create() => new ConfigEnvironment().Create();

        [Fact]
        public void Delete() => new ConfigEnvironment().Delete();

        [Fact]
        public async Task DontSaveWithoutEvents()
        {
            // nothing is setup, because nothing should be called when no events have been added
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<ConfigEnvironmentDomainObjectTests>>(MockBehavior.Strict);

            var domainObject = new ConfigEnvironment();

            await domainObject.Save(store.Object,
                                    history.Object,
                                    logger.Object,
                                    _metrics);
        }

        [Fact]
        public void ImportNoKeys() => new ConfigEnvironment().ImportKeys(new DtoConfigKey[0]);

        [Fact]
        public void ImportNullKeys() => new ConfigEnvironment().ImportKeys(null);

        [Fact]
        public void ModifyNoKeys() => new ConfigEnvironment().ModifyKeys(new ConfigKeyAction[0]);

        [Fact]
        public void ModifyNullKeys() => new ConfigEnvironment().ModifyKeys(null);
    }
}