using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Metrics;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Parsing;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Bechtle.A365.ConfigService.Projection.DomainEventHandlers;
using Bechtle.A365.ConfigService.Projection.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.EventHandlerTests
{
    public class ConfigurationBuiltHandlerTests
    {
        public ConfigurationBuiltHandlerTests()
        {
            _compiler = new ConfigurationCompiler(new Mock<ILogger<ConfigurationCompiler>>().Object);
            _parser = new AntlrConfigurationParser(new Mock<ILogger<AntlrConfigurationParser>>().Object);
            _translator = new JsonTranslator();
            _metrics = AppMetrics.CreateDefaultBuilder().Build();
        }

        /// <summary>
        ///     test-data for <see cref="HandleDomainEventWithIncorrectTarget" />
        /// </summary>
        public static IEnumerable<object[]> IncorrectTargetData => new[]
        {
            new object[] {new EnvironmentIdentifier(null, null), new StructureIdentifier(null, 0)},
            new object[] {new EnvironmentIdentifier("", ""), new StructureIdentifier("", 0)},
            new object[] {new EnvironmentIdentifier(null, null), new StructureIdentifier(null, -1)},
            new object[] {new EnvironmentIdentifier("", ""), new StructureIdentifier("", -1)},
            new object[] {new EnvironmentIdentifier(null, null), new StructureIdentifier(null, int.MaxValue)},
            new object[] {new EnvironmentIdentifier("", ""), new StructureIdentifier("", int.MaxValue)},
            new object[] {new EnvironmentIdentifier(null, null), new StructureIdentifier(null, int.MinValue)},
            new object[] {new EnvironmentIdentifier("", ""), new StructureIdentifier("", int.MinValue)},
            new object[] {new EnvironmentIdentifier(null, null), new StructureIdentifier(null, 1)},
            new object[] {new EnvironmentIdentifier("", ""), new StructureIdentifier("", 1)},
        };

        /// <summary>
        ///     test-data for <see cref="HandleDomainEventWithTargetNotFound" />
        /// </summary>
        public static IEnumerable<object[]> TargetMatrixData => new[]
        {
            new object[] {false, false},
            new object[] {true, false},
            new object[] {false, true}
        };

        private readonly IConfigurationCompiler _compiler;
        private readonly IConfigurationParser _parser;
        private readonly IJsonTranslator _translator;
        private readonly IMetrics _metrics;

        private IConfigurationDatabase MockConfigurationDatabase(Dictionary<string, string> structureData,
                                                                 Dictionary<string, string> structureVariables,
                                                                 Dictionary<string, string> environmentData)
        {
            var mock = new Mock<IConfigurationDatabase>();

            mock.Setup(db => db.Connect()).ReturnsAsync(Result.Success());

            mock.Setup(db => db.GetStructure(It.IsAny<StructureIdentifier>()))
                .ReturnsAsync((StructureIdentifier identifier)
                                  => Result.Success(
                                      new StructureSnapshot(identifier,
                                                            structureData,
                                                            structureVariables)));

            mock.Setup(db => db.GetEnvironmentWithInheritance(It.IsAny<EnvironmentIdentifier>()))
                .ReturnsAsync((EnvironmentIdentifier identifier)
                                  => Result.Success(
                                      new EnvironmentSnapshot(identifier,
                                                              environmentData)));

            mock.Setup(db => db.SaveConfiguration(It.IsAny<EnvironmentSnapshot>(),
                                                  It.IsAny<StructureSnapshot>(),
                                                  It.IsAny<IDictionary<string, string>>(),
                                                  It.IsAny<string>(),
                                                  It.IsAny<IEnumerable<string>>(),
                                                  It.IsAny<DateTime?>(),
                                                  It.IsAny<DateTime?>()))
                .ReturnsAsync(Result.Success());

            return mock.Object;
        }

        /// <summary>
        ///     check for exception when one or both identifiers result in data not being found
        /// </summary>
        /// <param name="envFound"></param>
        /// <param name="structFound"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(TargetMatrixData))]
        public async Task HandleDomainEventWithTargetNotFound(bool envFound, bool structFound)
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect()).ReturnsAsync(Result.Success());

            if (structFound)
                dbMock.Setup(db => db.GetStructure(It.IsAny<StructureIdentifier>()))
                      .ReturnsAsync(
                          (StructureIdentifier identifier) =>
                              Result.Success(new StructureSnapshot(identifier,
                                                                   new Dictionary<string, string>(),
                                                                   new Dictionary<string, string>())));
            else
                dbMock.Setup(db => db.GetStructure(It.IsAny<StructureIdentifier>()))
                      .ReturnsAsync((StructureIdentifier identifier) => Result.Error<StructureSnapshot>("structure not found", ErrorCode.NotFound));

            if (envFound)
                dbMock.Setup(db => db.GetEnvironmentWithInheritance(It.IsAny<EnvironmentIdentifier>()))
                      .ReturnsAsync(
                          (EnvironmentIdentifier identifier) =>
                              Result.Success(new EnvironmentSnapshot(identifier,
                                                                     new Dictionary<string, string>())));
            else
                dbMock.Setup(db => db.GetEnvironmentWithInheritance(It.IsAny<EnvironmentIdentifier>()))
                      .ReturnsAsync((EnvironmentIdentifier identifier) => Result.Error<EnvironmentSnapshot>("environment not found", ErrorCode.NotFound));

            var database = dbMock.Object;

            var domainEvent = new Mock<ConfigurationBuilt>(() => new ConfigurationBuilt(new ConfigurationIdentifier(
                                                                                            new EnvironmentIdentifier("env-cat", "env-name"),
                                                                                            new StructureIdentifier("struct-name", 1)),
                                                                                        DateTime.MinValue,
                                                                                        DateTime.MaxValue)).Object;

            var handler = new ConfigurationBuiltHandler(database,
                                                        _compiler,
                                                        _parser,
                                                        _translator,
                                                        new Mock<IEventBusService>().Object,
                                                        _metrics,
                                                        new Mock<ILogger<ConfigurationBuiltHandler>>().Object);

            // @TODO: this should ideally be a more specific exception (StructureNotFoundException / ItemNotFoundException)
            await Assert.ThrowsAsync<Exception>(() => handler.HandleDomainEvent(domainEvent));
        }

        [Theory]
        [MemberData(nameof(IncorrectTargetData))]
        public async Task HandleDomainEventWithIncorrectTarget(EnvironmentIdentifier envId, StructureIdentifier structId)
        {
            var dbMock = new Mock<IConfigurationDatabase>(MockBehavior.Strict);

            dbMock.Setup(db => db.Connect()).ReturnsAsync(Result.Success());

            dbMock.Setup(db => db.GetStructure(It.IsAny<StructureIdentifier>()))
                  .ReturnsAsync((StructureIdentifier identifier) => Result.Error<StructureSnapshot>("structure not found", ErrorCode.NotFound));

            dbMock.Setup(db => db.GetEnvironmentWithInheritance(It.IsAny<EnvironmentIdentifier>()))
                  .ReturnsAsync((EnvironmentIdentifier identifier) => Result.Error<EnvironmentSnapshot>("environment not found", ErrorCode.NotFound));

            var database = dbMock.Object;

            var domainEvent = new Mock<ConfigurationBuilt>(() => new ConfigurationBuilt(new ConfigurationIdentifier(envId, structId),
                                                                                        DateTime.MinValue,
                                                                                        DateTime.MaxValue)).Object;

            var handler = new ConfigurationBuiltHandler(database,
                                                        _compiler,
                                                        _parser,
                                                        _translator,
                                                        new Mock<IEventBusService>().Object,
                                                        _metrics,
                                                        new Mock<ILogger<ConfigurationBuiltHandler>>().Object);

            // @TODO: this should ideally be a more specific exception (StructureNotFoundException / ItemNotFoundException)
            await Assert.ThrowsAsync<Exception>(() => handler.HandleDomainEvent(domainEvent));
        }

        [Fact]
        public async Task HandleFilledDomainEvent()
        {
            var database = MockConfigurationDatabase(new Dictionary<string, string>
                                                     {
                                                         {"A", "{{Foo/*}}"},
                                                         {"B", "{{Lorem/*}}"},
                                                         {"C", "CVal"}
                                                     },
                                                     new Dictionary<string, string>
                                                     {
                                                         {"VarFoo", "True"},
                                                         {"VarBar", "False"}
                                                     },
                                                     new Dictionary<string, string>
                                                     {
                                                         {"Foo/Bar/Baz", "True"},
                                                         {"Foo/Bar/Qux", "False"},
                                                         {"Lorem/0000", "ipsum"},
                                                         {"Lorem/0001", "dolor"},
                                                         {"Lorem/0002", "sit"},
                                                         {"Lorem/0003", "amet"}
                                                     });

            var domainEvent = new Mock<ConfigurationBuilt>(() => new ConfigurationBuilt(new ConfigurationIdentifier(
                                                                                            new EnvironmentIdentifier("env-foo", "env-bar"),
                                                                                            new StructureIdentifier("struct-foo", 42)),
                                                                                        DateTime.MinValue,
                                                                                        DateTime.MaxValue)).Object;

            var handler = new ConfigurationBuiltHandler(database,
                                                        _compiler,
                                                        _parser,
                                                        _translator,
                                                        new Mock<IEventBusService>().Object,
                                                        _metrics,
                                                        new Mock<ILogger<ConfigurationBuiltHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleEmptyDomainEvent()
        {
            var database = MockConfigurationDatabase(new Dictionary<string, string>(),
                                                     new Dictionary<string, string>(),
                                                     new Dictionary<string, string>());

            var domainEvent = new Mock<ConfigurationBuilt>(() => new ConfigurationBuilt(new ConfigurationIdentifier(
                                                                                            new EnvironmentIdentifier("env-foo", "env-bar"),
                                                                                            new StructureIdentifier("struct-foo", 42)),
                                                                                        DateTime.MinValue,
                                                                                        DateTime.MaxValue)).Object;

            var handler = new ConfigurationBuiltHandler(database,
                                                        _compiler,
                                                        _parser,
                                                        _translator,
                                                        new Mock<IEventBusService>().Object,
                                                        _metrics,
                                                        new Mock<ILogger<ConfigurationBuiltHandler>>().Object);

            await handler.HandleDomainEvent(domainEvent);
        }

        [Fact]
        public async Task HandleNullDomainEvent()
        {
            var database = MockConfigurationDatabase(new Dictionary<string, string>(),
                                                     new Dictionary<string, string>(),
                                                     new Dictionary<string, string>());

            var handler = new ConfigurationBuiltHandler(database,
                                                        _compiler,
                                                        _parser,
                                                        _translator,
                                                        new Mock<IEventBusService>().Object,
                                                        _metrics,
                                                        new Mock<ILogger<ConfigurationBuiltHandler>>().Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleDomainEvent(null));
        }

        /// <summary>
        ///     check if the handler can be created using mocks that fail on access
        /// </summary>
        [Fact]
        public void HandlerCreatedWithNonFunctionalValues()
            => Assert.NotNull(new ConfigurationBuiltHandler(new Mock<IConfigurationDatabase>(MockBehavior.Strict).Object,
                                                            new Mock<IConfigurationCompiler>(MockBehavior.Strict).Object,
                                                            new Mock<IConfigurationParser>(MockBehavior.Strict).Object,
                                                            new Mock<IJsonTranslator>(MockBehavior.Strict).Object,
                                                            new Mock<IEventBusService>(MockBehavior.Strict).Object,
                                                            new Mock<IMetrics>(MockBehavior.Strict).Object,
                                                            new Mock<ILogger<ConfigurationBuiltHandler>>(MockBehavior.Strict).Object));

        /// <summary>
        ///     check if the handler can be created using these options
        /// </summary>
        [Fact]
        public void HandlerCreatedWithoutValues()
            => Assert.NotNull(new ConfigurationBuiltHandler(null, null, null, null, null, null, null));
    }
}