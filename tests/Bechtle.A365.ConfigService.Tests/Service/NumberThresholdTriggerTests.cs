using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.Implementations.SnapshotTriggers;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service
{
    public class NumberThresholdTriggerTests
    {
        [Fact]
        public void ConfigureWithEmptySection()
        {
            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string, string>("ConnectionName", "UnitTest"),
                                    new KeyValuePair<string, string>("MaxLiveQueueSize", "8"),
                                    new KeyValuePair<string, string>("ReadBatchSize", "64"),
                                    new KeyValuePair<string, string>("Stream", "ConfigStream"),
                                    new KeyValuePair<string, string>("Uri", "tcp://admin:changeit@localhost:2113"),
                                })
                                .Build();

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .Configure<EventStoreConnectionConfiguration>(configuration)
                           .BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<NumberThresholdSnapshotTrigger>());
        }

        [Fact]
        public void CreateInstance()
        {
            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string, string>("ConnectionName", "UnitTest"),
                                    new KeyValuePair<string, string>("MaxLiveQueueSize", "8"),
                                    new KeyValuePair<string, string>("ReadBatchSize", "64"),
                                    new KeyValuePair<string, string>("Stream", "ConfigStream"),
                                    new KeyValuePair<string, string>("Uri", "tcp://admin:changeit@localhost:2113"),
                                })
                                .Build();

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .Configure<EventStoreConnectionConfiguration>(configuration)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            Assert.NotNull(instance);
        }

        [Fact]
        [SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Assumption is that no exceptions are thrown")]
        public void DisposeObject()
        {
            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string, string>("ConnectionName", "UnitTest"),
                                    new KeyValuePair<string, string>("MaxLiveQueueSize", "8"),
                                    new KeyValuePair<string, string>("ReadBatchSize", "64"),
                                    new KeyValuePair<string, string>("Stream", "ConfigStream"),
                                    new KeyValuePair<string, string>("Uri", "tcp://admin:changeit@localhost:2113"),
                                })
                                .Build();

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(new Mock<IEventStore>(MockBehavior.Strict).Object)
                           .AddSingleton(new Mock<ISnapshotStore>(MockBehavior.Strict).Object)
                           .Configure<EventStoreConnectionConfiguration>(configuration)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            instance.Dispose();
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

            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string, string>("ConnectionName", "UnitTest"),
                                    new KeyValuePair<string, string>("MaxLiveQueueSize", "8"),
                                    new KeyValuePair<string, string>("ReadBatchSize", "64"),
                                    new KeyValuePair<string, string>("Stream", "ConfigStream"),
                                    new KeyValuePair<string, string>("Uri", "tcp://admin:changeit@localhost:2113"),
                                })
                                .Build();

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(esMock.Object)
                           .AddSingleton(ssMock.Object)
                           .Configure<EventStoreConnectionConfiguration>(configuration)
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

        [Fact]
        public async Task StartInstanceWithoutActionRequired()
        {
            var esMock = new Mock<IEventStore>(MockBehavior.Strict);
            esMock.Setup(e => e.GetCurrentEventNumber())
                  .ReturnsAsync(0)
                  .Verifiable("Current EventNumber was not retrieved");

            esMock.SetupAdd(e => e.EventAppeared += It.IsAny<EventHandler<(StoreSubscription, StoredEvent)>>())
                  .Verifiable("EventStore.EventAppeared was not subscribed");

            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            ssMock.Setup(s => s.GetLatestSnapshotNumbers())
                  .ReturnsAsync(Result.Success(0L))
                  .Verifiable("Latest Snapshot was not retrieved");

            var configuration = new ConfigurationBuilder()
                                .AddInMemoryCollection(new[]
                                {
                                    new KeyValuePair<string, string>("ConnectionName", "UnitTest"),
                                    new KeyValuePair<string, string>("MaxLiveQueueSize", "8"),
                                    new KeyValuePair<string, string>("ReadBatchSize", "64"),
                                    new KeyValuePair<string, string>("Stream", "ConfigStream"),
                                    new KeyValuePair<string, string>("Uri", "tcp://admin:changeit@localhost:2113"),
                                })
                                .Build();

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<NumberThresholdSnapshotTrigger>()
                           .AddSingleton(esMock.Object)
                           .AddSingleton(ssMock.Object)
                           .Configure<EventStoreConnectionConfiguration>(configuration)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<NumberThresholdSnapshotTrigger>();

            instance.Configure(new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Max", "42")
            }).Build());

            await instance.Start(CancellationToken.None);

            esMock.Verify();
        }
    }
}