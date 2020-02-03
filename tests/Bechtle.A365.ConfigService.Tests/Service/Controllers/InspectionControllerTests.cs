using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Implementations;
using Bechtle.A365.ConfigService.Interfaces.Stores;
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
                                                  .AddMetrics()
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
    }
}