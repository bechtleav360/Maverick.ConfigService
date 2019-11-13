using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class IncrementalSnapshotServiceTests
    {
        [Fact]
        public void CreateInstance()
        {
            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<IncrementalSnapshotService>()
                           .AddSingleton(ssMock.Object)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<IncrementalSnapshotService>();

            Assert.NotNull(instance);
        }

        [Fact]
        public void QueueSnapshot()
        {
            IncrementalSnapshotService.QueueSnapshot(new StreamedObjectSnapshot());
        }

        [Fact]
        public void QueueNullSnapshot()
        {
            IncrementalSnapshotService.QueueSnapshot(null);
        }

        [Fact]
        public async Task ExecuteInstance()
        {
            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            ssMock.Setup(s => s.SaveSnapshots(It.IsAny<IList<StreamedObjectSnapshot>>()))
                  .ReturnsAsync(Result.Success);

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<IncrementalSnapshotService>()
                           .AddSingleton(ssMock.Object)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<IncrementalSnapshotService>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await instance.StartAsync(cts.Token);
            await instance.StopAsync(cts.Token);
        }

        [Fact]
        public async Task ExecuteInstanceWithQueuedSnapshot()
        {
            var ssMock = new Mock<ISnapshotStore>(MockBehavior.Strict);

            ssMock.Setup(s => s.SaveSnapshots(It.IsAny<IList<StreamedObjectSnapshot>>()))
                  .ReturnsAsync(Result.Success)
                  .Verifiable("SaveSnapshots was never called");

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddSingleton<IncrementalSnapshotService>()
                           .AddSingleton(ssMock.Object)
                           .BuildServiceProvider();

            var instance = provider.GetRequiredService<IncrementalSnapshotService>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await instance.StartAsync(cts.Token);

            IncrementalSnapshotService.QueueSnapshot(new StreamedObjectSnapshot
            {
                Identifier = "Snapshot1",
                DataType = "Test-Snapshot",
                Version = 4711,
                JsonData = "{}"
            });

            await Task.Delay(TimeSpan.FromSeconds(21), cts.Token);

            await instance.StopAsync(cts.Token);

            ssMock.Verify();
        }
    }
}