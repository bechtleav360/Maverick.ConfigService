using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class HistoryControllerTests : ControllerTests<HistoryController>
    {
        private readonly Mock<IEventStore> _eventStore = new Mock<IEventStore>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override HistoryController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new HistoryController(
                provider,
                provider.GetService<ILogger<HistoryController>>(),
                _eventStore.Object);
        }

        [Fact]
        public async Task BlameEnvironment()
        {
            _eventStore.Setup(es => es.ReplayEventsAsStream(
                                  It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                  It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                  It.IsAny<int>(),
                                  It.IsAny<StreamDirection>(),
                                  It.IsAny<long>()))
                       .Returns((Func<(StoredEvent, DomainEventMetadata), bool> filter,
                                 Func<(StoredEvent, DomainEvent), bool> stream,
                                 int batch,
                                 StreamDirection direction,
                                 long maxVersion) =>
                       {
                           var events = new (StoredEvent, DomainEvent)[]
                           {
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 1,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 2,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Baz"), new[]
                                   {
                                       ConfigKeyAction.Set("Foo/Bar/Baz1", "Que2?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz2", "Que2?!")
                                   })),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 3,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[]
                                   {
                                       ConfigKeyAction.Delete("Foo/Bar/Baz1"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz3", "Que?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz4", "Que?!"),
                                       new ConfigKeyAction((ConfigKeyActionType) 42, "Foo", "Bar", "Baz", "Que?!")
                                   }))
                           };

                           foreach (var tuple in events) stream(tuple);

                           return Task.CompletedTask;
                       })
                       .Verifiable("events not replayed");

            var result = await TestAction<OkObjectResult>(c => c.BlameEnvironment("Foo", "Bar"));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task BlameEnvironmentFilter()
        {
            _eventStore.Setup(es => es.ReplayEventsAsStream(
                                  It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                  It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                  It.IsAny<int>(),
                                  It.IsAny<StreamDirection>(),
                                  It.IsAny<long>()))
                       .Returns((Func<(StoredEvent, DomainEventMetadata), bool> filter,
                                 Func<(StoredEvent, DomainEvent), bool> stream,
                                 int batch,
                                 StreamDirection direction,
                                 long maxVersion) =>
                       {
                           Assert.False(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentCreated),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Foo", "Bar").ToString()}
                                              }
                                          })),
                               "event not filtered based on metadata (wrong EventType)");

                           Assert.True(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentKeysModified),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Foo", "Bar").ToString()}
                                              }
                                          })),
                               "event wrongly filtered");

                           Assert.False(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentKeysModified),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Bar", "Baz").ToString()}
                                              }
                                          })),
                               "event not filtered based on metadata (wrong Metadata.Identifier)");

                           return Task.CompletedTask;
                       })
                       .Verifiable("events not replayed");

            await TestAction<OkObjectResult>(c => c.BlameEnvironment("Foo", "Bar"));
        }

        [Fact]
        public async Task GetEnvHistory()
        {
            _eventStore.Setup(es => es.ReplayEventsAsStream(
                                  It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                  It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                  It.IsAny<int>(),
                                  It.IsAny<StreamDirection>(),
                                  It.IsAny<long>()))
                       .Returns((Func<(StoredEvent, DomainEventMetadata), bool> filter,
                                 Func<(StoredEvent, DomainEvent), bool> stream,
                                 int batch,
                                 StreamDirection direction,
                                 long maxVersion) =>
                       {
                           var events = new (StoredEvent, DomainEvent)[]
                           {
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 1,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 2,
                                       EventType = nameof(EnvironmentKeysModified),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Baz"), new[]
                                   {
                                       ConfigKeyAction.Set("Foo/Bar/Baz1", "Que2?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz2", "Que2?!")
                                   })),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 3,
                                       EventType = nameof(EnvironmentKeysModified),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[]
                                   {
                                       ConfigKeyAction.Delete("Foo/Bar/Baz1"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz3", "Que?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz4", "Que?!"),
                                       new ConfigKeyAction((ConfigKeyActionType) 42, "Foo", "Bar", "Baz", "Que?!")
                                   }))
                           };

                           foreach (var tuple in events) stream(tuple);

                           return Task.CompletedTask;
                       })
                       .Verifiable("events not replayed");

            var result = await TestAction<OkObjectResult>(c => c.GetEnvironmentHistory("Foo", "Bar"));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetEnvHistoryFilter()
        {
            _eventStore.Setup(es => es.ReplayEventsAsStream(
                                  It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                  It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                  It.IsAny<int>(),
                                  It.IsAny<StreamDirection>(),
                                  It.IsAny<long>()))
                       .Returns((Func<(StoredEvent, DomainEventMetadata), bool> filter,
                                 Func<(StoredEvent, DomainEvent), bool> stream,
                                 int batch,
                                 StreamDirection direction,
                                 long maxVersion) =>
                       {
                           Assert.False(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentCreated),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Foo", "Bar").ToString()}
                                              }
                                          })),
                               "event not filtered based on metadata (wrong EventType)");

                           Assert.True(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentKeysModified),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Foo", "Bar").ToString()}
                                              }
                                          })),
                               "event wrongly filtered");

                           Assert.False(
                               filter((new StoredEvent
                                          {
                                              Data = new byte[0],
                                              EventId = Guid.NewGuid(),
                                              EventNumber = 1,
                                              EventType = nameof(EnvironmentKeysModified),
                                              Metadata = new byte[0],
                                              UtcTime = DateTime.UtcNow
                                          },
                                          new DomainEventMetadata
                                          {
                                              Filters =
                                              {
                                                  {KnownDomainEventMetadata.Identifier, new EnvironmentIdentifier("Bar", "Baz").ToString()}
                                              }
                                          })),
                               "event not filtered based on metadata (wrong Metadata.Identifier)");

                           return Task.CompletedTask;
                       })
                       .Verifiable("events not replayed");

            await TestAction<OkObjectResult>(c => c.GetEnvironmentHistory("Foo", "Bar"));
        }

        [Fact]
        public async Task GetEnvHistoryFiltered()
        {
            _eventStore.Setup(es => es.ReplayEventsAsStream(
                                  It.IsAny<Func<(StoredEvent, DomainEventMetadata), bool>>(),
                                  It.IsAny<Func<(StoredEvent, DomainEvent), bool>>(),
                                  It.IsAny<int>(),
                                  It.IsAny<StreamDirection>(),
                                  It.IsAny<long>()))
                       .Returns((Func<(StoredEvent, DomainEventMetadata), bool> filter,
                                 Func<(StoredEvent, DomainEvent), bool> stream,
                                 int batch,
                                 StreamDirection direction,
                                 long maxVersion) =>
                       {
                           var events = new (StoredEvent, DomainEvent)[]
                           {
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 1,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar"))),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 2,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Baz"), new[]
                                   {
                                       ConfigKeyAction.Set("Foo/Bar/Baz1", "Que2?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz2", "Que2?!")
                                   })),
                               (new StoredEvent
                                   {
                                       Data = new byte[0],
                                       EventId = Guid.NewGuid(),
                                       EventNumber = 3,
                                       EventType = nameof(EnvironmentCreated),
                                       Metadata = new byte[0],
                                       UtcTime = DateTime.UtcNow
                                   },
                                   new EnvironmentKeysModified(new EnvironmentIdentifier("Foo", "Bar"), new[]
                                   {
                                       ConfigKeyAction.Delete("Foo/Bar/Baz1"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz3", "Que?!"),
                                       ConfigKeyAction.Set("Foo/Bar/Baz4", "Que?!"),
                                       new ConfigKeyAction((ConfigKeyActionType) 42, "Foo", "Bar", "Baz", "Que?!")
                                   }))
                           };

                           foreach (var tuple in events) stream(tuple);

                           return Task.CompletedTask;
                       })
                       .Verifiable("events not replayed");

            var result = await TestAction<OkObjectResult>(c => c.GetEnvironmentHistory("Foo", "Bar", "Foo"));

            Assert.NotNull(result.Value);
        }
    }
}