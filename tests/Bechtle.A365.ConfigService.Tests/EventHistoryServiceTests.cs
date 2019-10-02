using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class MemoryEventHistoryServiceTests
    {
        public MemoryEventHistoryServiceTests()
        {
            _metrics = new MetricsBuilder().Build();
        }

        private readonly IMetrics _metrics;

        // @TODO: duplicate this test, but return ProjectedEventMetadata and execute the filters in ReplayEventsAsStream
        [Fact]
        public async Task FirstEventIsNew()
        {
            var loggerMock = new Mock<ILogger<MemoryEventHistoryService>>();

            var eventStoreMock = new Mock<IEventStore>(MockBehavior.Strict);
            eventStoreMock.Setup(e => e.ReplayEventsAsStream(It.IsAny<Func<RecordedEvent, bool>>(),
                                                             It.IsAny<Func<(RecordedEvent, DomainEvent), bool>>(),
                                                             It.IsAny<int>(),
                                                             It.IsAny<StreamDirection>()))
                          .Returns(Task.CompletedTask);

            var domainEventMock = new Mock<DomainEvent>(MockBehavior.Strict);
            domainEventMock.Setup(d => d.EventType)
                           .Returns("DomainEventMock");

            var projectionStoreMock = new Mock<IProjectionStore>(MockBehavior.Strict);
            projectionStoreMock.Setup(s => s.Metadata.GetProjectedEventMetadata(It.IsAny<Expression<Func<ProjectedEventMetadata, bool>>>(),
                                                                                It.IsAny<string>()))
                               .ReturnsAsync(() => Result.Success<IList<ProjectedEventMetadata>>(new List<ProjectedEventMetadata>()));

            var service = new MemoryEventHistoryService(loggerMock.Object,
                                                        eventStoreMock.Object,
                                                        projectionStoreMock.Object,
                                                        _metrics);

            Assert.Equal(EventStatus.Unknown, await service.GetEventStatus(domainEventMock.Object));
        }
    }
}