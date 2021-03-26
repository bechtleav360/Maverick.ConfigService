using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class StructureProjectionStoreTests
    {
        public StructureProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<StructureProjectionStore>>();
        }

        private readonly ILogger<StructureProjectionStore> _logger;

        [Fact]
        public async Task CreateNewStructure()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) => Result.Success(str))
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync(4711)
                      .Verifiable();

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.Create(new StructureIdentifier("Foo", 42),
                                            new Dictionary<string, string> {{"Foo", "Bar"}},
                                            new Dictionary<string, string> {{"Bar", "Baz"}});

            Assert.False(result.IsError, "result.IsError");

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task DeleteVariables()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync(4711)
                      .Verifiable();

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.DeleteVariables(new StructureIdentifier("Foo", 42), new[] {"Bar"});

            Assert.False(result.IsError, "result.IsError");

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigStructureList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new ConfigStructureList();
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(list);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigStructureList>())
                             .ReturnsAsync(() => Result.Success(new ConfigStructureList()))
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigStructureList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new ConfigStructureList();
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 41),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 43),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 return Result.Success(list);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailableVersions()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigStructureList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new ConfigStructureList();
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Bar", 40),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 41),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 return Result.Success(list);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetAvailableVersions("Foo", QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.Equal(2, result.Data.Count);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetKeys(new StructureIdentifier("Foo", 42), QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string>
                                                                        {
                                                                            {"Bar", "BarValue"},
                                                                            {"Baz", "BazValue"},
                                                                            {"Foo", "FooValue"}
                                                                        },
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetKeys(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetVariables()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetVariables(new StructureIdentifier("Foo", 42), QueryRange.All);

            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetVariablesPaged()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "FooValue"}},
                                                                        new Dictionary<string, string>
                                                                        {
                                                                            {"Bar", "BarValue"},
                                                                            {"Baz", "BazValue"},
                                                                            {"Foo", "FooValue"}
                                                                        }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.GetVariables(new StructureIdentifier("Foo", 42), QueryRange.Make(1, 1));

            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Baz", "BazValue"), result.Data.First());

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task UpdateVariables()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigStructure str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42),
                                                                        new Dictionary<string, string> {{"Foo", "Bar"}},
                                                                        new Dictionary<string, string> {{"Bar", "Baz"}}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 return Result.Success(str);
                             })
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync(4711)
                      .Verifiable();

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new StructureProjectionStore(_logger,
                                                     domainObjectStore.Object,
                                                     eventStore.Object,
                                                     new ICommandValidator[0]);

            var result = await store.UpdateVariables(new StructureIdentifier("Foo", 42), new Dictionary<string, string> {{"Bar", "Boo"}});

            Assert.False(result.IsError, "result.IsError");

            domainObjectStore.Verify();
            eventStore.Verify();
        }
    }
}