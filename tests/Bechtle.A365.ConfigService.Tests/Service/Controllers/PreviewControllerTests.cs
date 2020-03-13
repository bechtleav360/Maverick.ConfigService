using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
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
    public class PreviewControllerTests : ControllerTests<PreviewController>
    {
        private readonly Mock<IConfigurationCompiler> _compiler = new Mock<IConfigurationCompiler>(MockBehavior.Strict);
        private readonly Mock<IConfigurationParser> _parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
        private readonly Mock<IJsonTranslator> _translator = new Mock<IJsonTranslator>(MockBehavior.Strict);
        private readonly Mock<IProjectionStore> _projectionStore = new Mock<IProjectionStore>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override PreviewController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new PreviewController(
                provider,
                provider.GetService<ILogger<PreviewController>>(),
                _compiler.Object,
                _projectionStore.Object,
                _parser.Object,
                _translator.Object);
        }

        [Fact]
        public async Task PreviewConfiguration()
        {
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
                     .Verifiable("config not compiled");

            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                       .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                       .Verifiable("compilation-result not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            Assert.IsType<JsonElement>(result.Value);
            _projectionStore.Verify();
            _compiler.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationCompilerThrows()
        {
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

            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("environment keys not retrieved");

            _compiler.Setup(c => c.Compile(It.IsAny<EnvironmentCompilationInfo>(), It.IsAny<StructureCompilationInfo>(), It.IsAny<IConfigurationParser>()))
                     .Throws<Exception>()
                     .Verifiable("config not compiled");

            var result = await TestAction<OkObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            Assert.IsType<JsonElement>(result.Value);
            _projectionStore.Verify();
            _compiler.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationContainerNull()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.PreviewConfiguration(null));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task PreviewConfigurationEnvironmentNotFound()
        {
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

            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("environment keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationEnvironmentNull()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.PreviewConfiguration(new PreviewContainer
            {
                Environment = null,
                Structure = new StructurePreview()
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task PreviewConfigurationStructureKeysNotFound()
        {
            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("structure keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationStructureNull()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.PreviewConfiguration(new PreviewContainer
            {
                Environment = new EnvironmentPreview(),
                Structure = null
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task PreviewConfigurationStructureVariablesNotFound()
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

            var result = await TestAction<ObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationTranslatorThrows()
        {
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
                     .Verifiable("config not compiled");

            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                       .Throws<Exception>()
                       .Verifiable("compilation-result not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.PreviewConfiguration("Foo", "Bar", "Foo", 42));

            Assert.NotNull(result.Value);
            Assert.IsType<JsonElement>(result.Value);
            _projectionStore.Verify();
            _compiler.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationUsingExistingData()
        {
            _projectionStore.Setup(s => s.Environments.GetKeys(It.IsAny<EnvironmentKeyQueryParameters>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("existing keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetKeys(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("existing structure keys not retrieved");

            _projectionStore.Setup(s => s.Structures.GetVariables(It.IsAny<StructureIdentifier>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("existing structure variables not retrieved");

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
                     .Verifiable("config not compiled");

            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                       .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                       .Verifiable("compiled config not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.PreviewConfiguration(new PreviewContainer
            {
                Environment = new EnvironmentPreview
                {
                    Category = "Foo",
                    Name = "Bar"
                },
                Structure = new StructurePreview
                {
                    Name = "Foo",
                    Version = "42"
                }
            }));

            Assert.NotNull(result.Value);
            Assert.IsType<PreviewResult>(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task PreviewConfigurationUsingGivenData()
        {
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
                     .Verifiable("config not compiled");

            _translator.Setup(t => t.ToJson(It.IsAny<IDictionary<string, string>>()))
                       .Returns(() => JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement)
                       .Verifiable("compiled config not translated to json");

            var result = await TestAction<OkObjectResult>(c => c.PreviewConfiguration(new PreviewContainer
            {
                Environment = new EnvironmentPreview
                {
                    Keys = new Dictionary<string, string> {{"Foo", "Bar"}}
                },
                Structure = new StructurePreview
                {
                    Keys = new Dictionary<string, object> {{"Foo", "Bar"}},
                    Variables = new Dictionary<string, object> {{"Foo", "Bar"}}
                }
            }));

            Assert.NotNull(result.Value);
            Assert.IsType<PreviewResult>(result.Value);
            _projectionStore.Verify();
        }
    }
}