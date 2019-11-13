using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class NumberThresholdTriggerTests
    {
        [Fact]
        public void CreateInstance()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new ConfigServiceConfiguration
                           {
                               EventStoreConnection = new EventStoreConnectionConfiguration
                               {
                                   ConnectionName = "UnitTest",
                                   MaxLiveQueueSize = 8,
                                   ReadBatchSize = 64,
                                   Stream = "ConfigStream",
                                   Uri = "tcp://admin:changeit@localhost:2113"
                               }
                           })
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            Assert.NotNull(instance);
        }

        [Fact]
        public void ConfigureWithEmptySection()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new ConfigServiceConfiguration
                           {
                               EventStoreConnection = new EventStoreConnectionConfiguration
                               {
                                   ConnectionName = "UnitTest",
                                   MaxLiveQueueSize = 8,
                                   ReadBatchSize = 64,
                                   Stream = "ConfigStream",
                                   Uri = "tcp://admin:changeit@localhost:2113"
                               }
                           })
                           .BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<NumberThresholdSnapshotTrigger>());
        }

        [Fact]
        public void DisposeObject()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new ConfigServiceConfiguration
                           {
                               EventStoreConnection = new EventStoreConnectionConfiguration
                               {
                                   ConnectionName = "UnitTest",
                                   MaxLiveQueueSize = 8,
                                   ReadBatchSize = 64,
                                   Stream = "ConfigStream",
                                   Uri = "tcp://admin:changeit@localhost:2113"
                               }
                           })
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            instance.Dispose();
        }

        [Fact]
        public async Task StartInstanceWithoutActionRequired()
        {
            var esMock = new Mock<IEventStore>(MockBehavior.Strict);
            esMock.Setup(e => e.GetCurrentEventNumber())
                  .ReturnsAsync(0)
                  .Verifiable("Current EventNumber was not retrieved");

            esMock.SetupAdd(e => e.EventAppeared += It.IsAny<EventHandler<(EventStoreSubscription, ResolvedEvent)>>())
                  .Verifiable("EventStore.EventAppeared was not subscribed");

            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            ssMock.Setup(s => s.GetLatestSnapshotNumbers())
                  .ReturnsAsync(Result.Success(0L))
                  .Verifiable("Latest Snapshot was not retrieved");

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(esMock.Object)
                           .AddSingleton(ssMock.Object)
                           .AddSingleton(new ConfigServiceConfiguration
                           {
                               EventStoreConnection = new EventStoreConnectionConfiguration
                               {
                                   ConnectionName = "UnitTest",
                                   MaxLiveQueueSize = 8,
                                   ReadBatchSize = 64,
                                   Stream = "ConfigStream",
                                   Uri = "tcp://admin:changeit@localhost:2113"
                               }
                           })
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            instance.Configure(new ConfigurationBuilder().Build());
            await instance.Start(CancellationToken.None);

            esMock.Verify();
        }

        [Fact]
        public async Task InstanceTriggersWhenThresholdReached()
        {
            var esMock = new Mock<IEventStore>(MockBehavior.Strict);
            esMock.Setup(e => e.GetCurrentEventNumber())
                  .ReturnsAsync(4711L)
                  .Verifiable("Current EventNumber was not retrieved");

            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            ssMock.Setup(s => s.GetLatestSnapshotNumbers())
                  .ReturnsAsync(Result.Success(0L))
                  .Verifiable("Latest Snapshot was not retrieved");

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(esMock.Object)
                           .AddSingleton(ssMock.Object)
                           .AddSingleton(new ConfigServiceConfiguration
                           {
                               EventStoreConnection = new EventStoreConnectionConfiguration
                               {
                                   ConnectionName = "UnitTest",
                                   MaxLiveQueueSize = 8,
                                   ReadBatchSize = 64,
                                   Stream = "ConfigStream",
                                   Uri = "tcp://admin:changeit@localhost:2113"
                               }
                           })
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            instance.Configure(new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Max", "42")
            }).Build());

            await Assert.RaisesAsync<EventArgs>(
                handler => instance.SnapshotTriggered += handler,
                handler => instance.SnapshotTriggered -= handler,
                () => instance.Start(CancellationToken.None));

            esMock.Verify();
        }
    }
}