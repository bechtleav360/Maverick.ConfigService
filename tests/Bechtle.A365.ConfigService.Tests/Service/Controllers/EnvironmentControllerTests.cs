using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Converters;
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
    public class EnvironmentControllerTests
    {
        public static IEnumerable<object[]> InvalidIdentifierParameters => new[]
        {
            new object[] {"", ""},
            new object[] {null, null},
            new object[] {"Foo", null},
            new object[] {null, "Bar"}
        };

        private readonly Mock<IProjectionStore> _projectionStoreMock;
        private readonly Mock<IJsonTranslator> _jsonTranslatorMock;
        private readonly IServiceCollection _services;
        private readonly IDictionary<string, string> _deferredConfiguration;

        public EnvironmentControllerTests()
        {
            _deferredConfiguration = new Dictionary<string, string>();

            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(_deferredConfiguration);

            _services = new ServiceCollection().AddLogging()
                                               .AddSingleton<IConfiguration>(p => configurationBuilder.Build())
                                               .AddMetrics();

            _projectionStoreMock = new Mock<IProjectionStore>();
            _jsonTranslatorMock = new Mock<IJsonTranslator>();
        }

        private EnvironmentController CreateController()
        {
            var provider = _services.BuildServiceProvider();

            return new EnvironmentController(provider,
                                             provider.GetService<ILogger<EnvironmentController>>(),
                                             _projectionStoreMock.Object,
                                             _jsonTranslatorMock.Object);
        }

        [Fact]
        public async Task AddEnvironment()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<AcceptedAtActionResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal(nameof(EnvironmentController.GetKeys), result.ActionName);
            Assert.Equal(RouteUtilities.ControllerName<EnvironmentController>(), result.ControllerName);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task AddEnvironmentWithoutParameters(string category, string name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.AddEnvironment(category, name));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task AddEnvironmentStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .Throws<Exception>()
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal((int) HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task AddEnvironmentProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.Create(It.IsAny<EnvironmentIdentifier>(), false))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-creation has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.AddEnvironment("Foo", "Bar"));

            Assert.Equal((int) HttpStatusCode.InternalServerError, result.StatusCode);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task DeleteEnvironment()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .ReturnsAsync(Result.Success)
                                .Verifiable("environment-deletion has not been triggered");

            await TestAction<AcceptedResult>(c => c.DeleteEnvironment("Foo", "Bar"));
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierParameters))]
        public async Task DeleteEnvironmentWithoutParameters(string category, string name)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.DeleteEnvironment(category, name));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task DeleteEnvironmentStoreThrows()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .Throws<Exception>()
                                .Verifiable();

            var result = await TestAction<ObjectResult>(c => c.DeleteEnvironment("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        [Fact]
        public async Task DeleteEnvironmentProviderError()
        {
            _projectionStoreMock.Setup(s => s.Environments.Delete(It.IsAny<EnvironmentIdentifier>()))
                                .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                                .Verifiable("environment-deletion has not been triggered");

            var result = await TestAction<ObjectResult>(c => c.DeleteEnvironment("Foo", "Bar"));

            Assert.NotNull(result.Value);
            _projectionStoreMock.Verify();
            _jsonTranslatorMock.Verify();
        }

        private async Task<TActionResult> TestAction<TActionResult>(Func<EnvironmentController, Task<IActionResult>> actionInvoker)
        {
            var result = await actionInvoker(CreateController());

            Assert.IsType<TActionResult>(result);

            return (TActionResult) result;
        }
    }
}