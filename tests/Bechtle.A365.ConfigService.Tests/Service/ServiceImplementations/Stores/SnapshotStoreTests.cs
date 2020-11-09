using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public abstract class SnapshotStoreTests : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
        }

        protected ISnapshotStore Store { get; set; }

        [Fact]
        public virtual async Task GetLatestSnapshotNumbers()
        {
            var esMock = new Mock<IEventStore>(MockBehavior.Strict);
            esMock.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                  .ReturnsAsync(1)
                  .Verifiable("Events not written to EventStore");

            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            item.Create();
            await item.WriteRecordedEvents(esMock.Object);

            var snapshot = item.CreateSnapshot();

            await Store.SaveSnapshots(new[] {snapshot});

            var result = await Store.GetLatestSnapshotNumbers();

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(1, result.Data);
        }

        [Fact]
        public virtual async Task GetLatestSnapshotNumbersEmpty()
        {
            var result = await Store.GetLatestSnapshotNumbers();

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(0, result.Data);
        }

        [Fact]
        public virtual async Task RetrieveDomainObjectSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            item.Create();
            item.AssignLayers(new[] {new LayerIdentifier("Foo")});

            var snapshot = item.CreateSnapshot();

            await Store.SaveSnapshots(new[] {snapshot});

            var result = await Store.GetSnapshot<ConfigEnvironment>(new EnvironmentIdentifier("Foo", "Bar").ToString());

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);
            Assert.Equal(snapshot, result.Data);
        }

        [Fact]
        public virtual async Task RetrieveGenericExistingSnapshot()
        {
            var snapshot = new DomainObjectSnapshot("UT-Type", "UT-Id", "{\"UT-Data\":4711}", 1, 2);

            await Store.SaveSnapshots(new[] {snapshot});

            var result = await Store.GetSnapshot("UT-Type", "UT-Id");

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);
        }

        [Fact]
        public virtual async Task RetrieveVersionConstrainedSnapshot()
        {
            var eventStore = new Mock<IEventStore>();

            var nextEventId = 0L;
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync(() => nextEventId++);

            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();
            await item.WriteRecordedEvents(eventStore.Object);
            var firstSnapshot = item.CreateSnapshot();

            item.AssignLayers(new[] {new LayerIdentifier("Foo")});
            await item.WriteRecordedEvents(eventStore.Object);
            var secondSnapshot = item.CreateSnapshot();

            item.AssignLayers(new[] {new LayerIdentifier("Bar")});
            await item.WriteRecordedEvents(eventStore.Object);
            var thirdSnapshot = item.CreateSnapshot();

            await Store.SaveSnapshots(new[] {firstSnapshot, secondSnapshot, thirdSnapshot});

            var result = await Store.GetSnapshot(nameof(ConfigEnvironment), item.Identifier.ToString(), 1);

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(secondSnapshot, result.Data);
        }

        [Fact]
        public virtual async Task RetrieveVersionConstrainedSnapshotGeneric()
        {
            var eventStore = new Mock<IEventStore>();

            var nextEventId = 0L;
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync(() => nextEventId++);

            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));

            item.Create();
            await item.WriteRecordedEvents(eventStore.Object);
            var firstSnapshot = item.CreateSnapshot();

            item.AssignLayers(new[] {new LayerIdentifier("Foo")});
            await item.WriteRecordedEvents(eventStore.Object);
            var secondSnapshot = item.CreateSnapshot();

            item.AssignLayers(new[] {new LayerIdentifier("Bar")});
            await item.WriteRecordedEvents(eventStore.Object);
            var thirdSnapshot = item.CreateSnapshot();

            await Store.SaveSnapshots(new[] {firstSnapshot, secondSnapshot, thirdSnapshot});

            var result = await Store.GetSnapshot<ConfigEnvironment>(item.Identifier.ToString(), 1);

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(secondSnapshot, result.Data);
        }

        [Fact]
        public virtual async Task StoreDomainObjectSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            item.Create();
            item.AssignLayers(new[] {new LayerIdentifier("Foo")});

            var snapshot = item.CreateSnapshot();

            var result = await Store.SaveSnapshots(new[] {snapshot});

            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public virtual async Task StoreGenericSnapshot()
        {
            var snapshot = new DomainObjectSnapshot("UT-Type", "UT-Id", "{\"UT-Data\":4711}", 1, 2);

            var result = await Store.SaveSnapshots(new[] {snapshot});

            Assert.False(result.IsError, "result.IsError");
        }
    }
}