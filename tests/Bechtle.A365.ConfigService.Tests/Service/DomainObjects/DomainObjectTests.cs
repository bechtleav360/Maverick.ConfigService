using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class DomainObjectTests
    {
        [Fact]
        public async Task NothingWrittenWithoutStoredEvents()
        {
            var domainObject = new DefaultDomainObject();
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var result = await domainObject.WriteRecordedEvents(store.Object);
            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public async Task IncrementVersionAfterWrite()
        {
            var domainObject = new DefaultDomainObject();
            domainObject.AddEvent(new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            var store = new Mock<IEventStore>(MockBehavior.Strict);
            store.Setup(s => s.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                 .ReturnsAsync(42);

            await domainObject.WriteRecordedEvents(store.Object);

            Assert.Equal(42, domainObject.CurrentVersion);
            Assert.Equal(42, domainObject.MetaVersion);
        }

        [Fact]
        public async Task QueueClearedAfterWrite()
        {
            var domainObject = new DefaultDomainObject();
            domainObject.AddEvent(new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            var store = new Mock<IEventStore>(MockBehavior.Strict);
            store.Setup(s => s.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                 .ReturnsAsync(42);

            await domainObject.WriteRecordedEvents(store.Object);

            Assert.Empty(domainObject.GetCapturedDomainEvents());
        }

        [Fact]
        public void GetHandlerMapping() => Assert.NotEmpty(new DefaultDomainObject().GetHandledEvents());

        [Fact]
        public void CacheItemPriorityNoException() => new DefaultDomainObject().GetCacheItemPriority();

        [Fact]
        public void SnapshotNoException() => new DefaultDomainObject().CreateSnapshot();

        [Fact]
        public void SnapshotIncludesSubProperties()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.Contains("\"FooBarSubProperty1\":4711", snapshot.JsonData, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SnapshotContainsVersion()
        {
            var domainObject = new DefaultDomainObject();
            domainObject.SetVersion(42, 4711);

            var snapshot = domainObject.CreateSnapshot();

            Assert.Equal(42, snapshot.Version);
            Assert.Equal(4711, snapshot.MetaVersion);
        }

        [Fact]
        public void SnapshotContainsIdentifier()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.Identifier), "string.IsNullOrWhiteSpace(snapshot.Identifier)");
        }

        [Fact]
        public void SnapshotContainsDataType()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.DataType), "string.IsNullOrWhiteSpace(snapshot.DataType)");
        }

        [Fact]
        public void SnapshotContainsJson()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.JsonData), "string.IsNullOrWhiteSpace(snapshot.JsonData)");
        }

        /// <summary>
        ///     instantiatable class of DomainObject.
        ///     used to test virtual methods with default-implementations.
        ///     all abstract methods have been left Un-Implemented and throw <see cref="NotImplementedException"/>
        /// </summary>
        private class DefaultDomainObject : DomainObject
        {
            /// <inheritdoc />
            public override long CalculateCacheSize() => throw new NotImplementedException();

            /// <inheritdoc />
            protected override void ApplySnapshotInternal(DomainObject domainObject) => throw new NotImplementedException();

            /// <inheritdoc />
            protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping() => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(EnvironmentCreated), e => true}
            };

            public void SetVersion(long current, long meta)
            {
                CurrentVersion = current;
                MetaVersion = meta;
            }

            public void AddEvent(DomainEvent e) => CapturedDomainEvents.Add(e);

            public List<DomainEvent> GetCapturedDomainEvents() => CapturedDomainEvents;

            /// <see cref="DomainObjectTests.SnapshotIncludesSubProperties"/>
            public int FooBarSubProperty1 { get; set; } = 4711;
        }
    }
}