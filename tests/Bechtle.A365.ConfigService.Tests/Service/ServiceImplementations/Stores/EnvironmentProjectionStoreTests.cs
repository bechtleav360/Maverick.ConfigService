using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class EnvironmentProjectionStoreTests
    {
        public EnvironmentProjectionStoreTests()
        {
            _logger = new ServiceCollection()
                      .AddLogging()
                      .BuildServiceProvider()
                      .GetRequiredService<ILogger<EnvironmentProjectionStore>>();
        }

        private readonly ILogger<EnvironmentProjectionStore> _logger;

        [Fact]
        public async Task CreateNewEnvironment()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) => Result.Success(str))
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.Create(new EnvironmentIdentifier("Foo", "Bar"), false);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(str.Identifier),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.Delete(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAssignedLayers()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment str, string id, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetAssignedLayers(new EnvironmentIdentifier("Foo", "Bar"));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChild()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment str, string id, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteChildSuggestion()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment str, string id, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo/Bar/B", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRoot()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment str, string id, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), string.Empty, QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAutocompleteRootPart()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment str, string id, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyAutoComplete(new EnvironmentIdentifier("Foo", "Bar"), "Foo", QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigEnvironmentList>(It.IsAny<long>()))
                             .ReturnsAsync((long v) =>
                             {
                                 var list = new ConfigEnvironmentList();
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailableEmpty()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigEnvironmentList>(It.IsAny<long>()))
                             .ReturnsAsync(() => Result.Success(new ConfigEnvironmentList()))
                             .Verifiable();

            var eventStore = new Mock<IEventStore>(MockBehavior.Strict);

            eventStore.Setup(es => es.ReplayEventsAsStream(
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEventMetadata Metadata), bool>>(),
                                 It.IsAny<Func<(StoredEvent StoredEvent, DomainEvent DomainEvent), bool>>(),
                                 It.IsAny<StreamDirection>(),
                                 It.IsAny<long>()))
                      .Returns(Task.CompletedTask);

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.All);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetAvailablePaged()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);
            domainObjectStore.Setup(dos => dos.ReplayObject<ConfigEnvironmentList>(It.IsAny<long>()))
                             .ReturnsAsync((long v) =>
                             {
                                 var list = new ConfigEnvironmentList();
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Baz")),
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetAvailable(QueryRange.Make(1, 1));

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeyObjects()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyObjects(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeyObjectsWithoutRoot()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeyObjects(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Filter = "Foo/",
                RemoveRoot = "Foo",
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeys()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeysPaged()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Range = QueryRange.Make(1, 1)
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
            Assert.Equal(new KeyValuePair<string, string>("Foo/Bar", "BarValue"), result.Data.First());

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeysPreferExactMatch()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Filter = "Foo/Ba",
                PreferExactMatch = "Foo/Ba",
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task GetKeysWithoutRoot()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Filter = "Foo/",
                RemoveRoot = "Foo",
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            domainObjectStore.Verify();
            eventStore.Verify();
        }

        [Fact]
        public async Task ResistDuplicateKeyErrors()
        {
            var domainObjectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string id, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("foo", "fooValue"),
                                         ConfigKeyAction.Set("foo/bar", "barValue"),
                                         ConfigKeyAction.Set("foo/bar/baz", "bazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 return Result.Success(layer);
                             })
                             .Verifiable();

            domainObjectStore.Setup(dos => dos.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>()))
                             .ReturnsAsync((ConfigEnvironment str, string id) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4710
                                 });
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
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

            var store = new EnvironmentProjectionStore(eventStore.Object,
                                                       domainObjectStore.Object,
                                                       _logger,
                                                       new ICommandValidator[0]);

            var result = await store.GetKeys(new KeyQueryParameters<EnvironmentIdentifier>
            {
                Identifier = new EnvironmentIdentifier("Foo", "Bar"),
                Range = QueryRange.All
            });

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);

            Assert.Equal(3, result.Data.Count);
            Assert.Equal("fooValue", result.Data["foo"]);
            Assert.Equal("barValue", result.Data["foo/bar"]);
            Assert.Equal("bazValue", result.Data["foo/bar/baz"]);

            domainObjectStore.Verify();
            eventStore.Verify();
        }
    }
}