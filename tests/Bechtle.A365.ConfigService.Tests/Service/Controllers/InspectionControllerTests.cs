using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class InspectionControllerTests : ControllerTests<InspectionController>
    {
        private readonly Mock<IConfigurationCompiler> _compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
        private readonly Mock<IConfigurationParser> _parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
        private readonly Mock<IJsonTranslator> _translator = new Mock<IJsonTranslator>(MockBehavior.Strict);
        private readonly Mock<IProjectionStore> _projectionStore = new Mock<IProjectionStore>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override InspectionController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new InspectionController(
                provider,
                provider.GetService<ILogger<InspectionController>>(),
                _compiler.Object,
                _parser.Object,
                _translator.Object,
                _projectionStore.Object);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task InspectStructureStructProviderError(int version)
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            if (version == 1)
            {
                _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                                .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                                {
                                    {"Foo", "Bar"}
                                }))
                                .Verifiable("structure keys not retrieved");

                _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                                .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("structure variables not retrieved");
            }
            else if (version == 2)
            {
                _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                                .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                                .Verifiable("structure keys not retrieved");

                _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                                .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                                {
                                    {"Foo", "Bar"}
                                }));
            }

            var result = await TestAction<ObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureAll()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetAvailableWithEnvironment(It.IsAny<EnvironmentIdentifier>(),
                                                                                     It.IsAny<DateTime>(),
                                                                                     It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>
                            {
                                new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                            new StructureIdentifier("Foo", 42),
                                                            0)
                            }))
                            .Verifiable("available configurations for environment not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetUsedConfigurationKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                                  It.IsAny<DateTime>(),
                                                                                  It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IEnumerable<string>>(new[] {"Foo"}))
                            .Verifiable("used keys for environment not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetUsedKeysPerStructureAll("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureAllConfigProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetAvailableWithEnvironment(It.IsAny<EnvironmentIdentifier>(),
                                                                                     It.IsAny<DateTime>(),
                                                                                     It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<ConfigurationIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available configurations for environment not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeysPerStructureAll("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureAllEnvironmentProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("environment keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeysPerStructureAll("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureAllInvalidCategory()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetUsedKeysPerStructureAll("", "Bar"));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetUsedKeysPerStructureAllInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetUsedKeysPerStructureAll("Foo", ""));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetUsedKeysPerStructureLatest()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetAvailableWithEnvironment(It.IsAny<EnvironmentIdentifier>(),
                                                                                     It.IsAny<DateTime>(),
                                                                                     It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>
                            {
                                new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                            new StructureIdentifier("Foo", 42),
                                                            0)
                            }))
                            .Verifiable("available configurations for environment not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetUsedConfigurationKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                                  It.IsAny<DateTime>(),
                                                                                  It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IEnumerable<string>>(new[] {"Foo"}))
                            .Verifiable("used keys for environment not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetUsedKeysPerStructureLatest("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureLatestConfigProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetAvailableWithEnvironment(It.IsAny<EnvironmentIdentifier>(),
                                                                                     It.IsAny<DateTime>(),
                                                                                     It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<ConfigurationIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available configurations for environment not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeysPerStructureLatest("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureLatestEnvironmentProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("environment keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeysPerStructureLatest("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysPerStructureLatestInvalidCategory()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetUsedKeysPerStructureLatest("", "Bar"));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetUsedKeysPerStructureLatestInvalidName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetUsedKeysPerStructureLatest("Foo", ""));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectStructure()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("structure keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("structure variables not retrieved");

            _compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                     .Returns(() => new CompilationResult(
                                  new Dictionary<string, string>
                                  {
                                      {"Foo", "Bar"}
                                  },
                                  new TraceResult[]
                                  {
                                      new KeyTraceResult
                                      {
                                          Key = "Foo",
                                          OriginalValue = "Bar",
                                          Errors = new string[0],
                                          Warnings = new string[0],
                                          Children = new TraceResult[0]
                                      }
                                  }))
                     .Verifiable("configuration not compiled");

            var result = await TestAction<OkObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<StructureInspectionResult>(result.Value);
            Assert.True(((StructureInspectionResult) result.Value).CompilationSuccessful);
            _projectionStore.Verify();
            _compiler.Verify();
        }

        [Fact]
        public async Task InspectStructureCompilerThrows()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("structure keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("structure variables not retrieved");

            _compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                     .Throws<Exception>()
                     .Verifiable("configuration not compiled");

            var result = await TestAction<OkObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<StructureInspectionResult>(result.Value);
            Assert.False(((StructureInspectionResult) result.Value).CompilationSuccessful);
            _projectionStore.Verify();
            _compiler.Verify();
        }

        [Fact]
        public async Task InspectStructureEnvProviderFails()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("environment keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task InspectStructureInvalidEnvCategory()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectStructure("", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectStructureInvalidEnvName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectStructure("Foo", "", "Foo", 42));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectStructureInvalidStructureName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectStructure("Foo", "Bar", "", 42));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectStructureInvalidStructureVersion()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 0));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectStructureStructProviderThrows()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<BadRequestObjectResult>(c => c.InspectStructure("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task InspectUploadedStructure()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string>
                       {
                           {"Foo", "Bar"}
                       })
                       .Verifiable("structure not translated");

            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                     .Returns(() => new CompilationResult(
                                  new Dictionary<string, string>
                                  {
                                      {"Foo", "Bar"}
                                  },
                                  new TraceResult[]
                                  {
                                      new KeyTraceResult
                                      {
                                          Key = "Foo",
                                          OriginalValue = "Bar",
                                          Children = new TraceResult[0],
                                          Warnings = new string[0],
                                          Errors = new string[0]
                                      }
                                  }))
                     .Verifiable("configuration not compiled");

            var result = await TestAction<OkObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object> {{"Foo", "Bar"}}
            }));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<StructureInspectionResult>(result.Value);
            Assert.True(((StructureInspectionResult) result.Value).CompilationSuccessful);
            _translator.Verify();
            _projectionStore.Verify();
        }

        [Fact]
        public async Task InspectUploadedStructureCompilationThrows()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string>
                       {
                           {"Foo", "Bar"}
                       })
                       .Verifiable("structure not translated");

            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                     .Throws<Exception>()
                     .Verifiable("configuration not compiled");

            var result = await TestAction<OkObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<StructureInspectionResult>(result.Value);
            Assert.False(((StructureInspectionResult) result.Value).CompilationSuccessful);
            _translator.Verify();
            _projectionStore.Verify();
        }

        [Fact]
        public async Task InspectUploadedStructureEmptyStructure()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectUploadedStructureEnvironmentProviderError()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Returns(() => new Dictionary<string, string>
                       {
                           {"Foo", "Bar"}
                       })
                       .Verifiable("structure not translated");

            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("environment keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
            _translator.Verify();
            _projectionStore.Verify();
        }

        [Fact]
        public async Task InspectUploadedStructureInvalidEnvCategory()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectUploadedStructure("", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectUploadedStructureInvalidEnvName()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectUploadedStructure("Foo", "", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task InspectUploadedStructureInvalidStructure()
        {
            _translator.Setup(t => t.ToDictionary(It.IsAny<JsonElement>()))
                       .Throws<Exception>()
                       .Verifiable("structure not translated");

            var result = await TestAction<BadRequestObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", new DtoStructure
            {
                Name = "Foo",
                Version = 42,
                Structure = JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement,
                Variables = new Dictionary<string, object>()
            }));

            Assert.NotNull(result.Value);
            _translator.Verify();
        }

        [Fact]
        public async Task InspectUploadedStructureNull()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.InspectUploadedStructure("Foo", "Bar", null));

            Assert.NotNull(result.Value);
        }
    }
}