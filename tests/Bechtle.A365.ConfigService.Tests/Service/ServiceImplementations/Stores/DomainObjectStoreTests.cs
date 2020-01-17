using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class DomainObjectStoreTests
    {
        [Fact]
        public async Task CachedItemBeyondMaxVersionReplayed()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:10")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);

            var item = new ConfigEnvironmentList();
            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 4711
            });

            memCache.Set(nameof(ConfigEnvironmentList), item);

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>(42);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);

            eventStore.Verify();
            snapshotStore.Verify();
        }

        [Fact]
        public async Task CachedItemExpires()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:05")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);
            snapshotStore.Setup(s => s.GetSnapshot<ConfigEnvironmentList>(It.IsAny<string>(), It.IsAny<long>()))
                         .ReturnsAsync(() => Result.Error<DomainObjectSnapshot>(string.Empty, ErrorCode.DbQueryError))
                         .Verifiable();

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            await store.ReplayObject<ConfigEnvironmentList>();

            Assert.True(memCache.TryGetValue(nameof(ConfigEnvironmentList), out _), "item not cached after replay");

            await Task.Delay(TimeSpan.FromSeconds(5));

            Assert.False(memCache.TryGetValue(nameof(ConfigEnvironmentList), out _), "item still in cache after duration");

            eventStore.Verify();
            snapshotStore.Verify();
        }

        [Fact]
        public async Task CachedItemReplayed()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:10")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);

            var item = new ConfigEnvironmentList();
            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });

            memCache.Set(nameof(ConfigEnvironmentList), item);

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>();

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);

            eventStore.Verify();
            snapshotStore.Verify();
        }

        [Fact]
        public async Task CatchEventStoreError()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:05")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Throws<Exception>()
                      .Verifiable();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);
            snapshotStore.Setup(s => s.GetSnapshot<ConfigEnvironmentList>(It.IsAny<string>(), It.IsAny<long>()))
                         .ReturnsAsync(() => Result.Error<DomainObjectSnapshot>(string.Empty, ErrorCode.DbQueryError))
                         .Verifiable();

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>();
            Assert.True(result.IsError);

            eventStore.Verify();
            snapshotStore.Verify();
        }

        [Fact]
        public async Task CatchSnapshotStoreError()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:05")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);
            snapshotStore.Setup(s => s.GetSnapshot<ConfigEnvironmentList>(It.IsAny<string>(), It.IsAny<long>()))
                         .Throws<Exception>()
                         .Verifiable();

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>();
            Assert.True(result.IsError);

            snapshotStore.Verify();
        }

        [Fact]
        public async Task ItemReplayedWithSnapshot()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:10")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            var item = new ConfigEnvironmentList();
            item.ApplyEvent(new ReplayedEvent
            {
                DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                UtcTime = DateTime.UtcNow,
                Version = 1
            });
            var snapshot = item.CreateSnapshot();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);
            snapshotStore.Setup(s => s.GetSnapshot<ConfigEnvironmentList>(It.IsAny<string>(), It.IsAny<long>()))
                         .ReturnsAsync(Result.Success(snapshot));

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>();

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);

            eventStore.Verify();
            snapshotStore.Verify();
        }

        [Fact]
        public async Task ItemRetrievedFromEventStore()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddMemoryCache()
                           .BuildServiceProvider();

            var config = new ConfigurationBuilder()
                         .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("MemoryCache:Local:Duration", "00:00:10")})
                         .Build();

            var logger = provider.GetRequiredService<ILogger<DomainObjectStore>>();
            var memCache = provider.GetRequiredService<IMemoryCache>();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                 It.IsAny<int>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            var snapshotStore = new Mock<ISnapshotStore>(MockBehavior.Strict);
            snapshotStore.Setup(s => s.GetSnapshot<ConfigEnvironmentList>(It.IsAny<string>(), It.IsAny<long>()))
                         .ReturnsAsync(() => Result.Error<DomainObjectSnapshot>(string.Empty, ErrorCode.DbQueryError))
                         .Verifiable();

            var store = new DomainObjectStore(eventStore.Object,
                                              snapshotStore.Object,
                                              memCache,
                                              config,
                                              logger);

            var result = await store.ReplayObject<ConfigEnvironmentList>();

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);

            eventStore.Verify();
            snapshotStore.Verify();
        }
    }
}