using System.Collections.Generic;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.DomainObjectTests
{
    public class BaseTests
    {
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

        public BaseTests()
        {
            _metrics = new MetricsBuilder().Build();
        }

        private readonly IMetrics _metrics;

        [Fact]
        public async Task DontSaveWithoutEvents()
        {
            // nothing is setup, because nothing should be called when no events have been added
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<StructureTests>>(MockBehavior.Strict);

            var domainObject = new Mock<DomainObject>();

            await domainObject.Object
                              .Save(store.Object,
                                    history.Object,
                                    logger.Object,
                                    _metrics);
        }

        [Theory]
        [MemberData(nameof(SkippedEventStates))]
        public async Task SkipEvents(EventStatus status)
        {
            // nothing is setup, because nothing should be called when no events have been added
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var logger = new ServiceCollection().AddLogging()
                                                .BuildServiceProvider()
                                                .GetRequiredService<ILogger<StructureTests>>();

            // setup history to return the desired state - all events have been seen and shouldn't be saved again
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            history.Setup(h => h.GetEventStatus(It.IsAny<DomainEvent>()))
                   .ReturnsAsync(status);

            var domainObject = new ConfigStructure().IdentifiedBy(new StructureIdentifier("Foo", 42))
                                                    .Create(new Dictionary<string, string>(),
                                                            new Dictionary<string, string>());

            await domainObject.Save(store.Object,
                                    history.Object,
                                    logger,
                                    _metrics);
        }

        [Theory]
        [MemberData(nameof(ProcessedEventStates))]
        public async Task ProcessEvents(EventStatus status)
        {
            var logger = new ServiceCollection().AddLogging()
                                                .BuildServiceProvider()
                                                .GetRequiredService<ILogger<StructureTests>>();

            // setup store to appear as if everything was written correctly
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            store.Setup(s => s.WriteEvent(It.IsAny<DomainEvent>()))
                 .Returns(Task.CompletedTask);

            // setup history to return the desired state - all events are new and should be saved
            var history = new Mock<IEventHistoryService>(MockBehavior.Strict);
            history.Setup(h => h.GetEventStatus(It.IsAny<DomainEvent>()))
                   .ReturnsAsync(status);

            var domainObject = new ConfigStructure().IdentifiedBy(new StructureIdentifier("Foo", 42))
                                                    .Create(new Dictionary<string, string>(),
                                                            new Dictionary<string, string>());

            await domainObject.Save(store.Object,
                                    history.Object,
                                    logger,
                                    _metrics);

            history.Verify(s => s.GetEventStatus(It.IsAny<StructureCreated>()), Times.Once);
            store.Verify(s => s.WriteEvent(It.IsAny<DomainEvent>()), Times.Once);
        }
    }
}