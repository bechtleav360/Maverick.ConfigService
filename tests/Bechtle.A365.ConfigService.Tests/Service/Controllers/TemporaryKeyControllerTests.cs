using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.Core.EventBus.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class TemporaryKeyControllerTests : ControllerTests<TemporaryKeyController>
    {
        private readonly Mock<IEventBus> _eventBus = new Mock<IEventBus>(MockBehavior.Strict);
        private readonly Mock<ITemporaryKeyStore> _keyStore = new Mock<ITemporaryKeyStore>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override TemporaryKeyController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddMetrics()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new TemporaryKeyController(
                provider,
                provider.GetService<ILogger<TemporaryKeyController>>(),
                _keyStore.Object,
                _eventBus.Object);
        }

        [Theory]
        [InlineData("Foo", 42, null)]
        [InlineData("Foo", 42, "")]
        [InlineData("Foo", -1, "Bar")]
        [InlineData("", 42, "Bar")]
        [InlineData(null, 42, "Bar")]
        public async Task GetInvalidParameters(string structure, int version, string key)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.Get(structure, version, key));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Get()
        {
            _keyStore.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(() => Result.Success("Foo"))
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.Get("Foo", 42, "Bar"));

            Assert.Equal("Foo", result.Value);
            _keyStore.Verify();
        }
    }
}