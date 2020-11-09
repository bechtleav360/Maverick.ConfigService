using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class ConfigurationProjectionStoreTests
    {
        private (ILogger<ConfigurationProjectionStore> logger,
            Mock<IDomainObjectStore> DomainObjectStore,
            Mock<IConfigurationCompiler> Compiler,
            Mock<IConfigurationParser> Parser,
            Mock<IJsonTranslator> Translator,
            Mock<IEventStore> EventStore,
            IEnumerable<ICommandValidator> Validators) CreateMocks()
            => (new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
                .GetRequiredService<ILogger<ConfigurationProjectionStore>>(),
                   new Mock<IDomainObjectStore>(MockBehavior.Strict),
                   new Mock<IConfigurationCompiler>(MockBehavior.Strict),
                   new Mock<IConfigurationParser>(MockBehavior.Strict),
                   new Mock<IJsonTranslator>(MockBehavior.Strict),
                   new Mock<IEventStore>(MockBehavior.Strict),
                   new List<ICommandValidator>());

        private void VerifySetups(params Mock[] mocks)
        {
            foreach (var mock in mocks)
                mock.Verify();
        }

        private ConfigurationIdentifier CreateConfigurationIdentifier(string envCategory = "Foo",
                                                                      string envName = "Bar",
                                                                      string structName = "Foo",
                                                                      int structVersion = 42,
                                                                      long version = 4711)
            => new ConfigurationIdentifier(new EnvironmentIdentifier(envCategory, envName),
                                           new StructureIdentifier(structName, structVersion),
                                           version);

        [Fact]
        public async Task BuildNewConfig()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<PreparedConfiguration>(), It.IsAny<string>()))
                             .ReturnsAsync((PreparedConfiguration config, string identifier) => Result.Success(config))
                             .Verifiable("new PreparedConfiguration not replayed");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment environment, string identifier, long version) => Result.Success(environment))
                             .Verifiable("ConfigEnvironment not replayed");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigStructure structure, string identifier, long version) => Result.Success(structure))
                             .Verifiable("ConfigStructure not replayed");

            var nextEvent = 4711;
            eventStore.Setup(es => es.WriteEvents(It.IsAny<IList<DomainEvent>>()))
                      .ReturnsAsync((IList<DomainEvent> events) => nextEvent++)
                      .Verifiable("no event has been written to ES");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.Build(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                       new StructureIdentifier("Foo", 42),
                                                                       4711), null, null);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public async Task GetAllAvailableEmpty()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(Result.Success(new PreparedConfigurationList()))
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailable(DateTime.UtcNow, QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAllAvailablePaged()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 1), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 2), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 3), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4713
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailable(DateTime.UtcNow, QueryRange.Make(1, 1));

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentEmpty()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier("Baz",
                                                                                                        structVersion: 1), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailableWithEnvironment(new EnvironmentIdentifier("Foo", "Bar"),
                                                                 DateTime.UtcNow,
                                                                 QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentPaged()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier("Baz",
                                                                                                        structVersion: 1), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 2, version: 4712), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 3, version: 4713), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4713
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailableWithEnvironment(new EnvironmentIdentifier("Foo", "Bar"),
                                                                 DateTime.UtcNow,
                                                                 QueryRange.Make(1, 1));

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetAvailableForStructureEmpty()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailableWithStructure(new StructureIdentifier("Imaginary", 42),
                                                               DateTime.UtcNow,
                                                               QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAvailableForStructurePaged()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(
                                                                              structName: "Imaginary",
                                                                              structVersion: 1),
                                                                          null,
                                                                          null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4711
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 2), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4712
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(structVersion: 3), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4713
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("PreparedConfigList not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetAvailableWithEnvironment(new EnvironmentIdentifier("Foo", "Bar"),
                                                                 DateTime.UtcNow,
                                                                 QueryRange.Make(1, 1));

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetConfigVersion()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<PreparedConfiguration>(), It.IsAny<string>()))
                             .ReturnsAsync((PreparedConfiguration config, string identifier) =>
                             {
                                 config.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(config.Identifier, config.ValidFrom, config.ValidTo),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 0
                                 });

                                 return Result.Success(config);
                             })
                             .Verifiable("new PreparedConfiguration not replayed");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetVersion(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task GetJson()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<PreparedConfiguration>(), It.IsAny<string>()))
                             .ReturnsAsync((PreparedConfiguration config, string identifier) =>
                             {
                                 config.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(config.Identifier, config.ValidFrom, config.ValidTo),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 0
                                 });

                                 return Result.Success(config);
                             })
                             .Verifiable("new PreparedConfiguration not replayed");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string identifier, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(layer.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(layer.Identifier, new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(layer);
                             }).Verifiable("Layers not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment env, string identifier, long version) =>
                             {
                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(env.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(env.Identifier, new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(env);
                             })
                             .Verifiable("ConfigEnvironment not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigStructure str, string identifier, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(
                                         str.Identifier,
                                         new Dictionary<string, string> {{"Foo", "Bar"}},
                                         new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });

                                 return Result.Success(str);
                             })
                             .Verifiable("ConfigStructure not retrieved");

            compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(),
                                          It.IsAny<StructureCompilationInfo>(),
                                          It.IsAny<IConfigurationParser>()))
                    .Returns((EnvironmentCompilationInfo envInfo, StructureCompilationInfo structInfo, IConfigurationParser p)
                                 => new CompilationResult(new Dictionary<string, string>
                                 {
                                     {"Foo", "FooValue"},
                                     {"Foo/Bar", "BarValue"},
                                     {"Foo/Bar/Baz", "BazValue"}
                                 }, new TraceResult[0]))
                    .Verifiable("configuration has not been compiled");

            translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                      .Returns(() => JsonDocument.Parse("{\"valid\":true}").RootElement)
                      .Verifiable("keys not converted to json");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetJson(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data.GetProperty("valid").GetBoolean(), "result.Data.GetProperty('valid').GetBoolean()");
        }

        [Fact]
        public async Task GetKeys()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<PreparedConfiguration>(), It.IsAny<string>()))
                             .ReturnsAsync((PreparedConfiguration config, string identifier) =>
                             {
                                 config.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(config.Identifier, config.ValidFrom, config.ValidTo),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 0
                                 });

                                 return Result.Success(config);
                             })
                             .Verifiable("new PreparedConfiguration not replayed");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string identifier, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(layer.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(layer.Identifier, new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(layer);
                             }).Verifiable("Layers not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment env, string identifier, long version) =>
                             {
                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(env.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(env.Identifier, new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(env);
                             })
                             .Verifiable("ConfigEnvironment not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigStructure str, string identifier, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(
                                         str.Identifier,
                                         new Dictionary<string, string> {{"Foo", "Bar"}},
                                         new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });

                                 return Result.Success(str);
                             })
                             .Verifiable("ConfigStructure not retrieved");

            compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(),
                                          It.IsAny<StructureCompilationInfo>(),
                                          It.IsAny<IConfigurationParser>()))
                    .Returns((EnvironmentCompilationInfo envInfo, StructureCompilationInfo structInfo, IConfigurationParser p)
                                 => new CompilationResult(new Dictionary<string, string>
                                 {
                                     {"Foo", "FooValue"},
                                     {"Foo/Bar", "BarValue"},
                                     {"Foo/Bar/Baz", "BazValue"}
                                 }, new TraceResult[0]))
                    .Verifiable("configuration has not been compiled");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task GetStale()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 1
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42), new Dictionary<string, string>
                                     {
                                         {"Foo", "Bar"}
                                     }, new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 5
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 6
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValueNew"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValueNew"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValueNew")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 7
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("config-list not retrieved");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetStale(QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetUsedKeys()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<PreparedConfiguration>(), It.IsAny<string>()))
                             .ReturnsAsync((PreparedConfiguration config, string identifier) =>
                             {
                                 config.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(config.Identifier, config.ValidFrom, config.ValidTo),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 0
                                 });

                                 return Result.Success(config);
                             })
                             .Verifiable("new PreparedConfiguration not replayed");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<EnvironmentLayer>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((EnvironmentLayer layer, string identifier, long version) =>
                             {
                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(layer.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 layer.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(layer.Identifier, new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(layer);
                             }).Verifiable("Layers not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigEnvironment>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigEnvironment env, string identifier, long version) =>
                             {
                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(env.Identifier),
                                     UtcTime = DateTime.UtcNow - TimeSpan.FromSeconds(1),
                                     Version = 1
                                 });

                                 env.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(env.Identifier, new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });

                                 return Result.Success(env);
                             })
                             .Verifiable("ConfigEnvironment not retrieved");

            domainObjectStore.Setup(s => s.ReplayObject(It.IsAny<ConfigStructure>(), It.IsAny<string>(), It.IsAny<long>()))
                             .ReturnsAsync((ConfigStructure str, string identifier, long version) =>
                             {
                                 str.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(
                                         str.Identifier,
                                         new Dictionary<string, string> {{"Foo", "Bar"}},
                                         new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });

                                 return Result.Success(str);
                             })
                             .Verifiable("ConfigStructure not retrieved");

            translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                      .Returns(() => JsonDocument.Parse("{}").RootElement)
                      .Verifiable("keys not translated to json");

            compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(),
                                          It.IsAny<StructureCompilationInfo>(),
                                          It.IsAny<IConfigurationParser>()))
                    .Returns((EnvironmentCompilationInfo envInfo, StructureCompilationInfo structInfo, IConfigurationParser p)
                                 => new CompilationResult(new Dictionary<string, string>
                                 {
                                     {"Foo", "FooValue"},
                                     {"Foo/Bar", "BarValue"},
                                     {"Foo/Bar/Baz", "BazValue"}
                                 }, new TraceResult[]
                                 {
                                     new KeyTraceResult
                                     {
                                         Key = "Foo",
                                         OriginalValue = "FooValue",
                                         Children = new TraceResult[]
                                             {new KeyTraceResult {Key = "Foo/Bar", OriginalValue = "BarValue", Children = new TraceResult[0]}}
                                     }
                                 }))
                    .Verifiable("configuration has not been compiled");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.GetUsedConfigurationKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task IsNotStale()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 1
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42), new Dictionary<string, string>
                                     {
                                         {"Foo", "Bar"}
                                     }, new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 5
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 6
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("config-list not retrieved");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.False(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStale()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 1
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42), new Dictionary<string, string>
                                     {
                                         {"Foo", "Bar"}
                                     }, new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 5
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new ConfigurationBuilt(CreateConfigurationIdentifier(), null, null),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 6
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysImported(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValueNew"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValueNew"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValueNew")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 7
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("config-list not retrieved");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStaleUnknown()
        {
            var (logger, domainObjectStore, compiler, parser, translator, eventStore, validators) = CreateMocks();

            domainObjectStore.Setup(s => s.ReplayObject<PreparedConfigurationList>())
                             .ReturnsAsync(() =>
                             {
                                 var list = new PreparedConfigurationList();

                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentCreated(new EnvironmentIdentifier("Foo", "Bar")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 1
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerCreated(new LayerIdentifier("Foo")),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 2
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayerKeysModified(new LayerIdentifier("Foo"), new[]
                                     {
                                         ConfigKeyAction.Set("Foo", "FooValue"),
                                         ConfigKeyAction.Set("Foo/Bar", "BarValue"),
                                         ConfigKeyAction.Set("Foo/Bar/Baz", "BazValue")
                                     }),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 3
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new EnvironmentLayersModified(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                 new List<LayerIdentifier> {new LayerIdentifier("Foo")}),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 4
                                 });
                                 list.ApplyEvent(new ReplayedEvent
                                 {
                                     DomainEvent = new StructureCreated(new StructureIdentifier("Foo", 42), new Dictionary<string, string>
                                     {
                                         {"Foo", "Bar"}
                                     }, new Dictionary<string, string>()),
                                     UtcTime = DateTime.UtcNow,
                                     Version = 5
                                 });

                                 return Result.Success(list);
                             })
                             .Verifiable("config-list not retrieved");

            var store = new ConfigurationProjectionStore(logger,
                                                         domainObjectStore.Object,
                                                         compiler.Object,
                                                         parser.Object,
                                                         translator.Object,
                                                         eventStore.Object,
                                                         validators);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectStore, compiler, parser, translator, eventStore);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }
    }
}