using System;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class ExportControllerTests : ControllerTests<ExportController>
    {
        private readonly Mock<IDataExporter> _dataExporter = new Mock<IDataExporter>();

        protected override ExportController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new ExportController(
                provider,
                provider.GetService<ILogger<ExportController>>(),
                _dataExporter.Object);
        }

        [Fact]
        public async Task ExportEnv()
        {
            _dataExporter.Setup(e => e.Export(It.IsAny<ExportDefinition>()))
                         .ReturnsAsync((ExportDefinition def) => Result.Success(new ConfigExport
                         {
                             Environments = def.Environments
                                               .Select(d => new EnvironmentExport
                                               {
                                                   Category = d.Category,
                                                   Name = d.Name,
                                                   Layers = new[] {new LayerIdentifier("Foo")}
                                               })
                                               .ToArray()
                         }))
                         .Verifiable("data not exported");

            var result = await TestAction<FileStreamResult>(c => c.Export(new ExportDefinition
            {
                Environments = new[] {new EnvironmentIdentifier("Foo", "Bar")},
                Layers = new LayerIdentifier[0]
            }));

            Assert.IsType<FileStreamResult>(result);
        }

        [Fact]
        public async Task ExportEnvDefinitionEmpty()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.Export(new ExportDefinition
            {
                Environments = new EnvironmentIdentifier[0],
                Layers = new LayerIdentifier[0]
            }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task ExportEnvDefinitionNull()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.Export(null));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task ExportEnvProviderError()
        {
            _dataExporter.Setup(e => e.Export(It.IsAny<ExportDefinition>()))
                         .ReturnsAsync(() => Result.Error<ConfigExport>("something went wrong", ErrorCode.DbQueryError))
                         .Verifiable("environments not exported");

            var result = await TestAction<ObjectResult>(c => c.Export(new ExportDefinition
            {
                Environments = new[] {new EnvironmentIdentifier("Foo", "Bar")},
                Layers = new LayerIdentifier[0]
            }));

            Assert.NotNull(result.Value);
            _dataExporter.Verify();
        }

        [Fact]
        public async Task ExportEnvStoreThrows()
        {
            _dataExporter.Setup(e => e.Export(It.IsAny<ExportDefinition>()))
                         .Throws<Exception>()
                         .Verifiable("environments not exported");

            var result = await TestAction<ObjectResult>(c => c.Export(new ExportDefinition
            {
                Environments = new[] {new EnvironmentIdentifier("Foo", "Bar")},
                Layers = new LayerIdentifier[0]
            }));

            Assert.NotNull(result.Value);
            _dataExporter.Verify();
        }
    }
}