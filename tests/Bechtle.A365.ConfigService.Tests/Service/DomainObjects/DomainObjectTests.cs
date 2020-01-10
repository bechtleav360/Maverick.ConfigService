using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.DomainObjects
{
    public class DomainObjectTests
    {
        /// <summary>
        ///     instantiatable class of DomainObject.
        ///     used to test virtual methods with default-implementations.
        ///     all abstract methods have been left Un-Implemented and throw <see cref="NotImplementedException" />
        /// </summary>
        private class DefaultDomainObject : DomainObject
        {
            /// <see cref="DomainObjectTests.SnapshotIncludesSubProperties" />
            public int FooBarSubProperty1 { get; set; } = 4711;

            public event EventHandler<EventArgs> EventReplayHandled;

            public event EventHandler<EventArgs> SnapshotAppliedInternally;

            public void AddEvent(DomainEvent e) => CapturedDomainEvents.Add(e);

            /// <inheritdoc />
            public override long CalculateCacheSize() => throw new NotImplementedException();

            public List<DomainEvent> GetCapturedDomainEvents() => CapturedDomainEvents;

            public void SetVersion(long current, long meta)
            {
                CurrentVersion = current;
                MetaVersion = meta;
            }

            /// <inheritdoc />
            protected override void ApplySnapshotInternal(DomainObject domainObject) => SnapshotAppliedInternally?.Invoke(this, EventArgs.Empty);

            /// <inheritdoc />
            protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping() => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {
                    typeof(EnvironmentCreated), e =>
                    {
                        EventReplayHandled?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                }
            };
        }

        [Fact]
        public void CacheItemPriorityNoException() => new DefaultDomainObject().GetCacheItemPriority();

        [Fact]
        public void EventReplayHandlerInvoked()
        {
            var domainObject = new DefaultDomainObject();

            Assert.RaisesAny<EventArgs>(e => domainObject.EventReplayHandled += e,
                                        e => domainObject.EventReplayHandled -= e,
                                        () => domainObject.ApplyEvent(new ReplayedEvent
                                        {
                                            Version = 42,
                                            UtcTime = DateTime.UtcNow,
                                            DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))
                                        }));
        }

        [Fact]
        public void GetHandlerMapping() => Assert.NotEmpty(new DefaultDomainObject().GetHandledEvents());

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
        public async Task NothingWrittenWithoutStoredEvents()
        {
            var domainObject = new DefaultDomainObject();
            var store = new Mock<IEventStore>(MockBehavior.Strict);
            var result = await domainObject.WriteRecordedEvents(store.Object);
            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public void NullDomainEventReplayed()
        {
            var domainObject = new DefaultDomainObject();

            var versions = (domainObject.CurrentVersion, domainObject.MetaVersion);

            domainObject.ApplyEvent(new ReplayedEvent
            {
                Version = 42,
                UtcTime = DateTime.UtcNow,
                DomainEvent = null
            });

            Assert.Equal(versions.CurrentVersion, domainObject.CurrentVersion);
            Assert.Equal(versions.MetaVersion, domainObject.MetaVersion);
        }

        [Fact]
        public void NullEventReplayed()
        {
            var domainObject = new DefaultDomainObject();

            var versions = (domainObject.CurrentVersion, domainObject.MetaVersion);

            domainObject.ApplyEvent(null);

            Assert.Equal(versions.CurrentVersion, domainObject.CurrentVersion);
            Assert.Equal(versions.MetaVersion, domainObject.MetaVersion);
        }

        [Fact]
        public void OldEventGiven()
        {
            var domainObject = new DefaultDomainObject();
            domainObject.SetVersion(100, 101);

            domainObject.ApplyEvent(new ReplayedEvent
            {
                Version = 42,
                UtcTime = DateTime.UtcNow,
                DomainEvent = new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))
            });

            Assert.Equal(100, domainObject.CurrentVersion);
            Assert.Equal(101, domainObject.MetaVersion);
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
        public void SnapshotAppliedInternally()
        {
            var domainObject = new DefaultDomainObject();
            var snapshot = domainObject.CreateSnapshot();

            snapshot.JsonData = "{\"FooBarSubProperty1\":4242}";
            snapshot.MetaVersion = 42;
            snapshot.Version = 42;

            Assert.RaisesAny<EventArgs>(e => domainObject.SnapshotAppliedInternally += e,
                                        e => domainObject.SnapshotAppliedInternally -= e,
                                        () => { domainObject.ApplySnapshot(snapshot); });

            Assert.Equal(42, domainObject.MetaVersion);
            Assert.Equal(42, domainObject.CurrentVersion);
        }

        [Fact]
        public void SnapshotContainsDataType()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.DataType), "string.IsNullOrWhiteSpace(snapshot.DataType)");
        }

        [Fact]
        public void SnapshotContainsIdentifier()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.Identifier), "string.IsNullOrWhiteSpace(snapshot.Identifier)");
        }

        [Fact]
        public void SnapshotContainsJson()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.False(string.IsNullOrWhiteSpace(snapshot.JsonData), "string.IsNullOrWhiteSpace(snapshot.JsonData)");
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
        public void SnapshotIncludesSubProperties()
        {
            var domainObject = new DefaultDomainObject();

            var snapshot = domainObject.CreateSnapshot();

            Assert.Contains("\"FooBarSubProperty1\":4711", snapshot.JsonData, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SnapshotNoException() => new DefaultDomainObject().CreateSnapshot();

        [Fact]
        public void UnhandledEventGiven()
        {
            var domainObject = new DefaultDomainObject();

            var versions = (domainObject.CurrentVersion, domainObject.MetaVersion);

            domainObject.ApplyEvent(new ReplayedEvent
            {
                Version = 42,
                UtcTime = DateTime.UtcNow,
                DomainEvent = new DefaultEnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))
            });

            Assert.Equal(versions.CurrentVersion, domainObject.CurrentVersion);
            Assert.Equal(42, domainObject.MetaVersion);
        }

        [Fact]
        public void UnknownSnapshotNotAccepted()
        {
            var domainObject = new DefaultDomainObject();

            var versions = (domainObject.CurrentVersion, domainObject.MetaVersion);
            domainObject.ApplySnapshot(new DomainObjectSnapshot
            {
                DataType = "ForgedDataType",
                Identifier = "Some-Identifier",
                JsonData = "{}",
                Version = long.MaxValue,
                MetaVersion = long.MaxValue
            });

            Assert.Equal(versions, (domainObject.CurrentVersion, domainObject.MetaVersion));
        }

        [Fact]
        public void ValidatesCapturedEvents()
        {
            var validator = new Mock<ICommandValidator>(MockBehavior.Strict);
            validator.Setup(v => v.ValidateDomainEvent(It.IsAny<EnvironmentCreated>()))
                     .Returns(Result.Success)
                     .Verifiable();

            var domainObject = new DefaultDomainObject();

            domainObject.AddEvent(new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")));

            domainObject.Validate(new List<ICommandValidator> {validator.Object});

            validator.Verify();
        }

        [Fact]
        public void VersionsIncrementedAfterReplay()
        {
            var domainObject = new DefaultDomainObject();

            domainObject.ApplyEvent(new ReplayedEvent
            {
                Version = 42,
                UtcTime = DateTime.UtcNow,
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))
            });

            Assert.Equal(42, domainObject.CurrentVersion);
            Assert.Equal(42, domainObject.MetaVersion);
        }
    }
}