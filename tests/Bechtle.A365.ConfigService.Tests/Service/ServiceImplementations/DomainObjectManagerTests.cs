using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Configuration;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Bechtle.A365.ServiceBase.EventStore.DomainEventBase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations
{
    public class DomainObjectManagerTests
    {
        private readonly Mock<IOptionsSnapshot<EventStoreConnectionConfiguration>> _configuration =
            new Mock<IOptionsSnapshot<EventStoreConnectionConfiguration>>(MockBehavior.Strict);

        private readonly Mock<IEventStore> _eventStore = new Mock<IEventStore>(MockBehavior.Strict);

        private readonly ILogger<DomainObjectManager> _logger = new NullLogger<DomainObjectManager>();

        private readonly Mock<IDomainObjectStore> _objectStore = new Mock<IDomainObjectStore>(MockBehavior.Strict);

        private readonly IEnumerable<ICommandValidator> _validators = new List<ICommandValidator>();

        private readonly Mock<IEventStoreOptionsProvider> _optionsProvider = new Mock<IEventStoreOptionsProvider>(MockBehavior.Strict);

        public DomainObjectManagerTests()
        {
            // initialize this, because we need it basically everywhere
            _configuration.Setup(x => x.Value)
                          .Returns(
                              new EventStoreConnectionConfiguration
                              {
                                  Stream = "eventstore-stream",
                                  Uri = "esdb://localhost:2113",
                                  ConnectionName = "unit-tests"
                              });
        }

        [Fact]
        public async Task AssignLayersToEnvironment()
            => await TestObjectModification<ConfigEnvironment, EnvironmentIdentifier, EnvironmentLayersModified>(
                   async manager => await manager.AssignEnvironmentLayers(
                                        new EnvironmentIdentifier("Foo", "Bar"),
                                        new List<LayerIdentifier>
                                        {
                                            new LayerIdentifier("Foo"),
                                            new LayerIdentifier("Bar")
                                        },
                                        CancellationToken.None),
                   new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")));

        [Fact]
        public async Task CreateConfiguration()
            => await TestObjectCreation<PreparedConfiguration, ConfigurationIdentifier, ConfigurationBuilt>(
                   async manager => await manager.CreateConfiguration(
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bar"),
                                            new StructureIdentifier("Foo", 42),
                                            42),
                                        null,
                                        null,
                                        CancellationToken.None));

        [Fact]
        public async Task CreateDefaultEnvironment()
            => await TestObjectCreation<ConfigEnvironment, EnvironmentIdentifier, DefaultEnvironmentCreated>(
                   async manager => await manager.CreateEnvironment(
                                        new EnvironmentIdentifier("Foo", "Bar"),
                                        true,
                                        CancellationToken.None));

        [Fact]
        public async Task CreateEnvironment()
            => await TestObjectCreation<ConfigEnvironment, EnvironmentIdentifier, EnvironmentCreated>(
                   async manager => await manager.CreateEnvironment(new EnvironmentIdentifier("Foo", "Bar"), CancellationToken.None));

        [Fact]
        public async Task CreateLayer()
            => await TestObjectCreation<EnvironmentLayer, LayerIdentifier, EnvironmentLayerCreated>(
                   async manager => await manager.CreateLayer(new LayerIdentifier("Foo"), CancellationToken.None));

        [Fact]
        public async Task CreateStructure()
            => await TestObjectCreation<ConfigStructure, StructureIdentifier, StructureCreated>(
                   async manager => await manager.CreateStructure(
                                        new StructureIdentifier("Foo", 42),
                                        new Dictionary<string, string> { { "Foo", "Bar" } },
                                        new Dictionary<string, string> { { "Bar", "Baz" } },
                                        CancellationToken.None));

        [Fact]
        public async Task DeleteEnvironment()
            => await TestObjectDeletion<ConfigEnvironment, EnvironmentIdentifier, EnvironmentDeleted>(
                   async manager => await manager.DeleteEnvironment(new EnvironmentIdentifier("Foo", "Bar"), CancellationToken.None),
                   new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar")));

        [Fact]
        public async Task DeleteLayer()
            => await TestObjectDeletion<EnvironmentLayer, LayerIdentifier, EnvironmentLayerDeleted>(
                   async manager => await manager.DeleteLayer(new LayerIdentifier("Foo"), CancellationToken.None),
                   new EnvironmentLayer(new LayerIdentifier("Foo")));

        [Fact]
        public async Task GetAllConfigurations()
        {
            _objectStore.Setup(
                            m => m.ListAll<PreparedConfiguration, ConfigurationIdentifier>(
                                It.IsAny<Func<ConfigurationIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<ConfigurationIdentifier>(
                                    new[]
                                    {
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bar"),
                                            new StructureIdentifier("Foo", 42),
                                            69),
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bar"),
                                            new StructureIdentifier("Foo", 43),
                                            70)
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<ConfigurationIdentifier>> result = await manager.GetConfigurations(QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetConfiguration()
        {
            var identifier = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 42),
                69);

            AssertLoadsObjectSuccessfully<PreparedConfiguration, ConfigurationIdentifier>(new PreparedConfiguration(identifier));

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.GetConfiguration(identifier, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task GetConfigurationsWithEnvironment()
        {
            _objectStore.Setup(
                            m => m.ListAll<PreparedConfiguration, ConfigurationIdentifier>(
                                It.IsAny<Func<ConfigurationIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<ConfigurationIdentifier>(
                                    new[]
                                    {
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bro"),
                                            new StructureIdentifier("Foo", 42),
                                            71),
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bro"),
                                            new StructureIdentifier("Foo", 43),
                                            72)
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<ConfigurationIdentifier>> result = await manager.GetConfigurations(
                                                                new EnvironmentIdentifier("Foo", "Bro"),
                                                                QueryRange.All,
                                                                CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetConfigurationsWithStructure()
        {
            _objectStore.Setup(
                            m => m.ListAll<PreparedConfiguration, ConfigurationIdentifier>(
                                It.IsAny<Func<ConfigurationIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<ConfigurationIdentifier>(
                                    new[]
                                    {
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bar"),
                                            new StructureIdentifier("Foo", 43),
                                            70),
                                        new ConfigurationIdentifier(
                                            new EnvironmentIdentifier("Foo", "Bro"),
                                            new StructureIdentifier("Foo", 43),
                                            72)
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<ConfigurationIdentifier>> result = await manager.GetConfigurations(
                                                                new StructureIdentifier("Foo", 43),
                                                                QueryRange.All,
                                                                CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetEnvironment()
        {
            var identifier = new EnvironmentIdentifier("Foo", "Bar");

            AssertLoadsObjectSuccessfully<ConfigEnvironment, EnvironmentIdentifier>(new ConfigEnvironment(identifier));

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.GetEnvironment(identifier, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task GetEnvironments()
        {
            _objectStore.Setup(
                            m => m.ListAll<ConfigEnvironment, EnvironmentIdentifier>(
                                It.IsAny<Func<EnvironmentIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<EnvironmentIdentifier>(
                                    new[]
                                    {
                                        new EnvironmentIdentifier("Foo", "Bar"),
                                        new EnvironmentIdentifier("Foo", "Baz")
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<EnvironmentIdentifier>> result = await manager.GetEnvironments(QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetLayer()
        {
            var identifier = new LayerIdentifier("Foo");

            AssertLoadsObjectSuccessfully<EnvironmentLayer, LayerIdentifier>(new EnvironmentLayer(identifier));

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.GetLayer(identifier, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task GetLayers()
        {
            _objectStore.Setup(
                            m => m.ListAll<EnvironmentLayer, LayerIdentifier>(
                                It.IsAny<Func<LayerIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<LayerIdentifier>(
                                    new[]
                                    {
                                        new LayerIdentifier("Foo"),
                                        new LayerIdentifier("Bar")
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<LayerIdentifier>> result = await manager.GetLayers(QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetStale()
        {
            var staleConfigId = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 42),
                69);

            var freshConfigId = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 43),
                70);

            _objectStore.Setup(
                            m => m.ListAll<PreparedConfiguration, ConfigurationIdentifier>(
                                It.IsAny<Func<ConfigurationIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<ConfigurationIdentifier>(
                                    new[]
                                    {
                                        staleConfigId,
                                        freshConfigId
                                    })))
                        .Verifiable("object-list not queried from object-store");

            _objectStore.Setup(m => m.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(It.Is<ConfigurationIdentifier>(id => id == staleConfigId)))
                        .ReturnsAsync(
                            Result.Success<IDictionary<string, string>>(
                                new Dictionary<string, string>
                                {
                                    { "stale", "true" }
                                }))
                        .Verifiable("metadata for stale config was not checked");

            _objectStore.Setup(m => m.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(It.Is<ConfigurationIdentifier>(id => id == freshConfigId)))
                        .ReturnsAsync(Result.Success<IDictionary<string, string>>(new Dictionary<string, string>()))
                        .Verifiable("metadata for fresh config was not checked");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<ConfigurationIdentifier>> result = await manager.GetStaleConfigurations(QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.Single(result.Data.Items);
            Assert.Equal(staleConfigId, result.Data.Items.First());
        }

        [Fact]
        public async Task GetStructure()
        {
            var identifier = new StructureIdentifier("Foo", 42);

            AssertLoadsObjectSuccessfully<ConfigStructure, StructureIdentifier>(new ConfigStructure(identifier));

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.GetStructure(identifier, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task GetStructures()
        {
            _objectStore.Setup(
                            m => m.ListAll<ConfigStructure, StructureIdentifier>(
                                It.IsAny<Func<StructureIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(
                            Result.Success(
                                new Page<StructureIdentifier>(
                                    new[]
                                    {
                                        new StructureIdentifier("Foo", 69),
                                        new StructureIdentifier("Bar", 42)
                                    })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<StructureIdentifier>> result = await manager.GetStructures(QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.NotEmpty(result.Data.Items);
        }

        [Fact]
        public async Task GetStructuresWithName()
        {
            _objectStore.Setup(
                            m => m.ListAll<ConfigStructure, StructureIdentifier>(
                                It.IsAny<Func<StructureIdentifier, bool>>(),
                                It.IsAny<QueryRange>()))
                        .ReturnsAsync(Result.Success(new Page<StructureIdentifier>(new[] { new StructureIdentifier("Foo", 69) })))
                        .Verifiable("object-list not queried from object-store");

            DomainObjectManager manager = CreateTestObject();
            IResult<Page<StructureIdentifier>> result = await manager.GetStructures("Foo", QueryRange.All, CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
            Assert.Single(result.Data.Items);
        }

        [Fact]
        public async Task ImportExistingLayer()
        {
            AssertLoadsObjectSuccessfully<EnvironmentLayer, LayerIdentifier>(new EnvironmentLayer(new LayerIdentifier("Foo")));
            AssertGetsProjectedVersion();
            AssertEventStoreOptionsLoaded();
            _eventStore.Setup(
                           e => e.WriteEventsAsync(
                               It.Is<IList<IDomainEvent>>(
                                   list => list.Count == 1
                                           && ((LateBindingDomainEvent<DomainEvent>)list[0]).Payload is EnvironmentLayerKeysImported),
                               It.IsAny<string>(),
                               It.Is<ExpectStreamPosition>(r => ((NumberedPosition)r.StreamPosition).EventNumber == 4711)))
                       .Returns(Task.CompletedTask)
                       .Verifiable("events were not written to stream");

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.ImportLayer(
                                 new LayerIdentifier("Foo"),
                                 new List<EnvironmentLayerKey>
                                 {
                                     new EnvironmentLayerKey("Foo", "Bar", string.Empty, string.Empty, 42),
                                     new EnvironmentLayerKey("Bar", "Baz", string.Empty, string.Empty, 42)
                                 },
                                 CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task ImportNewLayer()
        {
            AssertLoadsObject<EnvironmentLayer, LayerIdentifier>();
            AssertGetsProjectedVersion();
            AssertEventStoreOptionsLoaded();
            _eventStore.Setup(
                           e => e.WriteEventsAsync(
                               It.Is<IList<IDomainEvent>>(
                                   list => list.Count == 1
                                           && ((LateBindingDomainEvent<DomainEvent>)list[0]).Payload is EnvironmentLayerCreated),
                               It.IsAny<string>(),
                               It.Is<ExpectStreamPosition>(r => ((NumberedPosition)r.StreamPosition).EventNumber == 4711)))
                       .Returns(Task.CompletedTask)
                       .Verifiable("events were not written to stream");

            _eventStore.Setup(
                           e => e.WriteEventsAsync(
                               It.Is<IList<IDomainEvent>>(
                                   list => list.Count == 1
                                           && ((LateBindingDomainEvent<DomainEvent>)list[0]).Payload is EnvironmentLayerKeysImported),
                               It.IsAny<string>(),
                               It.Is<ExpectStreamPosition>(r => ((NumberedPosition)r.StreamPosition).EventNumber == 4712)))
                       .Returns(Task.CompletedTask)
                       .Verifiable("events were not written to stream");

            DomainObjectManager manager = CreateTestObject();
            IResult result = await manager.ImportLayer(
                                 new LayerIdentifier("Foo"),
                                 new List<EnvironmentLayerKey>
                                 {
                                     new EnvironmentLayerKey("Foo", "Bar", string.Empty, string.Empty, 42),
                                     new EnvironmentLayerKey("Bar", "Baz", string.Empty, string.Empty, 42)
                                 },
                                 CancellationToken.None);

            VerifySetups();
            AssertPositiveResult(result);
        }

        [Fact]
        public async Task IsNotStale()
        {
            var configId = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 42),
                69);

            AssertLoadsObjectSuccessfully<PreparedConfiguration, ConfigurationIdentifier>(new PreparedConfiguration(configId));
            AssertGetsProjectedVersion();

            _objectStore.Setup(m => m.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(It.IsAny<ConfigurationIdentifier>()))
                        .ReturnsAsync(
                            Result.Success<IDictionary<string, string>>(
                                new Dictionary<string, string>
                                {
                                    { "stale", "false" }
                                }))
                        .Verifiable("metadata for object not loaded");

            DomainObjectManager manager = CreateTestObject();
            IResult<bool> result = await manager.IsStale(configId);

            AssertPositiveResult(result);
            Assert.False(result.Data);
        }

        [Fact]
        public async Task IsStale()
        {
            var configId = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 42),
                69);

            AssertLoadsObjectSuccessfully<PreparedConfiguration, ConfigurationIdentifier>(new PreparedConfiguration(configId));
            AssertGetsProjectedVersion();

            _objectStore.Setup(m => m.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(It.IsAny<ConfigurationIdentifier>()))
                        .ReturnsAsync(
                            Result.Success<IDictionary<string, string>>(
                                new Dictionary<string, string>
                                {
                                    { "stale", "true" }
                                }))
                        .Verifiable("metadata for object not loaded");

            DomainObjectManager manager = CreateTestObject();
            IResult<bool> result = await manager.IsStale(configId);

            AssertPositiveResult(result);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task IsStaleWithoutMetadata()
        {
            var configId = new ConfigurationIdentifier(
                new EnvironmentIdentifier("Foo", "Bar"),
                new StructureIdentifier("Foo", 42),
                69);

            AssertLoadsObjectSuccessfully<PreparedConfiguration, ConfigurationIdentifier>(new PreparedConfiguration(configId));
            AssertGetsProjectedVersion();

            _objectStore.Setup(m => m.LoadMetadata<PreparedConfiguration, ConfigurationIdentifier>(It.IsAny<ConfigurationIdentifier>()))
                        .ReturnsAsync(
                            Result.Success<IDictionary<string, string>>(
                                new Dictionary<string, string>()))
                        .Verifiable("metadata for object not loaded");

            DomainObjectManager manager = CreateTestObject();
            IResult<bool> result = await manager.IsStale(configId);

            AssertPositiveResult(result);
            Assert.True(result.Data);
        }

        [Fact]
        public async Task ModifyLayerKeys()
            => await TestObjectModification<EnvironmentLayer, LayerIdentifier, EnvironmentLayerKeysModified>(
                   async manager => await manager.ModifyLayerKeys(
                                        new LayerIdentifier("Foo"),
                                        new List<ConfigKeyAction>
                                        {
                                            ConfigKeyAction.Set("Baz", "BazValue"),
                                            ConfigKeyAction.Delete("Foo")
                                        },
                                        CancellationToken.None),
                   new EnvironmentLayer(new LayerIdentifier("Foo"))
                   {
                       Keys = new Dictionary<string, EnvironmentLayerKey>
                       {
                           { "Foo", new EnvironmentLayerKey("Foo", "FooValue", string.Empty, string.Empty, 42) },
                           { "Bar", new EnvironmentLayerKey("Bar", "BarValue", string.Empty, string.Empty, 42) }
                       }
                   });

        [Fact]
        public async Task ModifyStructureVariables()
            => await TestObjectModification<ConfigStructure, StructureIdentifier, StructureVariablesModified>(
                   async manager => await manager.ModifyStructureVariables(
                                        new StructureIdentifier("Foo", 42),
                                        new List<ConfigKeyAction>
                                        {
                                            ConfigKeyAction.Set("Baz", "BazValue"),
                                            ConfigKeyAction.Delete("Foo")
                                        },
                                        CancellationToken.None),
                   new ConfigStructure(new StructureIdentifier("Foo", 42))
                   {
                       Keys = new Dictionary<string, string>
                       {
                           { "Foo", "FooValue" },
                           { "Bar", "BarValue" }
                       },
                       Variables = new Dictionary<string, string>
                       {
                           { "Foo", "FooValue" },
                           { "Bar", "BarValue" }
                       }
                   });

        private void AssertGetsProjectedVersion(long version = 4711)
            => _objectStore.Setup(m => m.GetProjectedVersion())
                           .ReturnsAsync(Result.Success(version))
                           .Verifiable("current event-version was not queried - writes are likely not safe");

        private void AssertLoadsObject<TDomainObject, TIdentifier>()
            where TDomainObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => _objectStore.Setup(m => m.Load<TDomainObject, TIdentifier>(It.IsAny<TIdentifier>()))
                           .ReturnsAsync(Result.Error<TDomainObject>("not found", ErrorCode.NotFound))
                           .Verifiable("no check if object already exists");

        private void AssertLoadsObjectSuccessfully<TDomainObject, TIdentifier>(TDomainObject domainObject)
            where TDomainObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => _objectStore.Setup(m => m.Load<TDomainObject, TIdentifier>(It.IsAny<TIdentifier>()))
                           .ReturnsAsync(Result.Success(domainObject))
                           .Verifiable("no check if object already exists");

        private static void AssertPositiveResult(IResult result)
        {
            Assert.NotNull(result);
            Assert.False(result.IsError, "result.IsError");
        }

        private void AssertWritesOneEvent<TEvent>(ulong expectedVersion = 4711)
        {
            _eventStore.Setup(
                           e => e.WriteEventsAsync(
                               It.Is<IList<IDomainEvent>>(
                                   domainEvents => domainEvents.Any(de => ((LateBindingDomainEvent<DomainEvent>)de).Payload is TEvent)),
                               It.IsAny<string>(),
                               It.Is<ExpectStreamPosition>(
                                   // check if we're writing with the EXACT position that we saved at the last projection
                                   // if this does not hold, writes could result in invalid state
                                   r => ((NumberedPosition)r.StreamPosition).EventNumber == expectedVersion)))
                       .Returns(Task.CompletedTask)
                       .Verifiable("events were not written to stream");
        }

        private void AssertEventStoreOptionsLoaded()
        {
            _optionsProvider.Setup(p => p.LoadConfiguration(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            _optionsProvider.Setup(p => p.EventSizeLimited)
                            .Returns(true);

            _optionsProvider.Setup(p => p.MaxEventSizeInBytes)
                            .Returns(long.MaxValue);
        }

        private DomainObjectManager CreateTestObject() => new DomainObjectManager(
            _objectStore.Object,
            _eventStore.Object,
            _optionsProvider.Object,
            _configuration.Object,
            _validators,
            _logger);

        /// <summary>
        ///     This is the meat of all DomainObjectManager.Create* methods, as they all function the same way.
        ///     They check if the item already exists, generate appropriate DomainEvents, validate them, get the last-projected event, and write to the ES.
        ///     The only difference between the DomainObjects being created is the DomainEvents that are generated
        /// </summary>
        /// <param name="testedFunction">function that calls the tested method of <see cref="DomainObjectManager" /></param>
        /// <typeparam name="TDomainObject">type of DomainObject being created</typeparam>
        /// <typeparam name="TIdentifier">type of Identifier used by <typeparamref name="TDomainObject" /></typeparam>
        /// <typeparam name="TEvent">type of event expected to be written to EventStore</typeparam>
        private async Task TestObjectCreation<TDomainObject, TIdentifier, TEvent>(Func<DomainObjectManager, Task<IResult>> testedFunction)
            where TDomainObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            AssertLoadsObject<TDomainObject, TIdentifier>();
            AssertGetsProjectedVersion();
            AssertWritesOneEvent<TEvent>();
            AssertEventStoreOptionsLoaded();

            DomainObjectManager manager = CreateTestObject();
            IResult result = await testedFunction(manager);

            VerifySetups();
            AssertPositiveResult(result);
        }

        private async Task TestObjectDeletion<TDomainObject, TIdentifier, TEvent>(
            Func<DomainObjectManager, Task<IResult>> testedFunction,
            TDomainObject domainObject)
            where TDomainObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => await TestObjectModification<TDomainObject, TIdentifier, TEvent>(testedFunction, domainObject);

        private async Task TestObjectModification<TDomainObject, TIdentifier, TEvent>(
            Func<DomainObjectManager, Task<IResult>> testedFunction,
            TDomainObject domainObject)
            where TDomainObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            AssertLoadsObjectSuccessfully<TDomainObject, TIdentifier>(domainObject);
            AssertGetsProjectedVersion();
            AssertWritesOneEvent<TEvent>();
            AssertEventStoreOptionsLoaded();

            DomainObjectManager manager = CreateTestObject();
            IResult result = await testedFunction(manager);

            VerifySetups();
            AssertPositiveResult(result);
        }

        private void VerifySetups()
        {
            _configuration.Verify();
            _eventStore.Verify();
            _objectStore.Verify();
        }
    }
}
