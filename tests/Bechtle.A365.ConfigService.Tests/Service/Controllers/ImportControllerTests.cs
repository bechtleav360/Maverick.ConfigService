using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class ImportControllerTests : ControllerTests<ImportController>
    {
        private readonly Mock<IDataImporter> _dataImporter = new Mock<IDataImporter>();

        protected override ImportController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new ImportController(
                provider,
                provider.GetService<ILogger<ImportController>>(),
                _dataImporter.Object);
        }

        [Fact]
        public async Task ImportEnv()
        {
            _dataImporter.Setup(i => i.Import(It.IsAny<ConfigExport>()))
                         .ReturnsAsync(Result.Success)
                         .Verifiable("nothing imported");

            await TestAction<AcceptedAtActionResult>(async c =>
            {
                await using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, new ConfigExport
                {
                    Environments = new[]
                    {
                        new EnvironmentExport
                        {
                            Category = "Foo",
                            Name = "Bar",
                            Keys = new[] {new EnvironmentKeyExport {Key = "Foo", Value = "Bar"}}
                        }
                    }
                });

                stream.Position = 0;

                return await c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            _dataImporter.Verify();
        }

        [Fact]
        public async Task ImportEnvProviderError()
        {
            _dataImporter.Setup(i => i.Import(It.IsAny<ConfigExport>()))
                         .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                         .Verifiable("nothing imported");

            var result = await TestAction<ObjectResult>(async c =>
            {
                await using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, new ConfigExport
                {
                    Environments = new[]
                    {
                        new EnvironmentExport
                        {
                            Category = "Foo",
                            Name = "Bar",
                            Keys = new[] {new EnvironmentKeyExport {Key = "Foo", Value = "Bar"}}
                        }
                    }
                });

                stream.Position = 0;

                return await c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            Assert.NotNull(result.Value);

            _dataImporter.Verify();
        }

        [Fact]
        public async Task ImportEnvStoreThrows()
        {
            _dataImporter.Setup(i => i.Import(It.IsAny<ConfigExport>()))
                         .Throws<Exception>()
                         .Verifiable("nothing imported");

            var result = await TestAction<ObjectResult>(async c =>
            {
                await using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, new ConfigExport
                {
                    Environments = new[]
                    {
                        new EnvironmentExport
                        {
                            Category = "Foo",
                            Name = "Bar",
                            Keys = new[] {new EnvironmentKeyExport {Key = "Foo", Value = "Bar"}}
                        }
                    }
                });

                stream.Position = 0;

                return await c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            Assert.NotNull(result.Value);

            _dataImporter.Verify();
        }

        [Fact]
        public async Task ImportInvalidFile()
        {
            var result = await TestAction<BadRequestObjectResult>(c =>
            {
                using var stream = new MemoryStream();
                stream.Write(Encoding.UTF8.GetBytes("[ \"is not a valid ConfigExport, and shall throw during deserialization\" ]"));
                stream.Position = 0;
                return c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task ImportNullJson()
        {
            var result = await TestAction<BadRequestObjectResult>(c =>
            {
                using var stream = new MemoryStream();
                stream.Write(Encoding.UTF8.GetBytes("null"));
                stream.Position = 0;
                return c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task ImportZeroLengthFile()
        {
            var result = await TestAction<BadRequestObjectResult>(c =>
            {
                using var stream = new MemoryStream();
                return c.Import(new FormFile(stream, 0, stream.Length, "export", "export"));
            });

            Assert.NotNull(result.Value);
        }
    }
}