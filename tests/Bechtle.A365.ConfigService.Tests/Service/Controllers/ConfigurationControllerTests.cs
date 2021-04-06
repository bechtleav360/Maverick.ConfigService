using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public class ConfigurationControllerTests : ControllerTests<ConfigurationController>
    {
        private readonly Mock<IProjectionStore> _projectionStore = new Mock<IProjectionStore>(MockBehavior.Strict);

        private readonly Mock<IEventBus> _eventBus = new Mock<IEventBus>(MockBehavior.Strict);

        /// <inheritdoc />
        protected override ConfigurationController CreateController()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                          .Build();

            var provider = new ServiceCollection().AddLogging()
                                                  .AddSingleton<IConfiguration>(configuration)
                                                  .BuildServiceProvider();

            return new ConfigurationController(
                provider,
                provider.GetService<ILogger<ConfigurationController>>(),
                _projectionStore.Object,
                _eventBus.Object);
        }

        [Fact]
        public async Task BuildConfiguration()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }))
                            .Verifiable("available environments not retrieved");

            _projectionStore.Setup(s => s.Configurations.IsStale(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                             new StructureIdentifier("Foo", 42),
                                                                                             0)))
                            .ReturnsAsync(() => Result.Success(true))
                            .Verifiable("staleness of configuration not checked");

            _projectionStore.Setup(s => s.Configurations.Build(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                           new StructureIdentifier("Foo", 42),
                                                                                           0),
                                                               DateTime.MinValue,
                                                               DateTime.MaxValue))
                            .ReturnsAsync(Result.Success)
                            .Verifiable("configuration not built");

            _eventBus.Setup(e => e.Publish(It.IsAny<EventMessage>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable("OnConfigurationPublished was not published");

            var result = await TestAction<AcceptedAtActionResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions
            {
                ValidFrom = DateTime.MinValue,
                ValidTo = DateTime.MaxValue
            }), c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.Equal(RouteUtilities.ControllerName<ConfigurationController>(), result.ControllerName);
            Assert.Equal(nameof(ConfigurationController.GetConfiguration), result.ActionName);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildConfigurationNotStale()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }))
                            .Verifiable("available environments not retrieved");

            _projectionStore.Setup(s => s.Configurations.IsStale(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                             new StructureIdentifier("Foo", 42),
                                                                                             0)))
                            .ReturnsAsync(() => Result.Success(false))
                            .Verifiable("staleness of configuration not checked");

            var result = await TestAction<AcceptedAtActionResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions
            {
                ValidFrom = DateTime.MinValue,
                ValidTo = DateTime.MaxValue
            }), c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.Equal(RouteUtilities.ControllerName<ConfigurationController>(), result.ControllerName);
            Assert.Equal(nameof(ConfigurationController.GetConfiguration), result.ActionName);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildEnvironmentProviderError()
        {
            _projectionStore.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<EnvironmentIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available environments not retrieved");

            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            var result = await TestAction<ObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }))
                            .Verifiable("available environments not retrieved");

            _projectionStore.Setup(s => s.Configurations.IsStale(It.IsAny<ConfigurationIdentifier>()))
                            .ReturnsAsync(() => Result.Success(true))
                            .Verifiable("staleness not checked");

            _projectionStore.Setup(s => s.Configurations.Build(It.IsAny<ConfigurationIdentifier>(), null, null))
                            .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                            .Verifiable("configuration not built");

            var result = await TestAction<ObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildStalenessProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(QueryRange.All))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }))
                            .Verifiable("available environments not retrieved");

            _projectionStore.Setup(s => s.Configurations.IsStale(It.IsAny<ConfigurationIdentifier>()))
                            .ReturnsAsync(() => Result.Error<bool>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("staleness not checked");

            var result = await TestAction<ObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildStructureProviderError()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<StructureIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }));

            var result = await TestAction<ObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildWithInvalidOptions()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions
            {
                ValidTo = DateTime.MinValue,
                ValidFrom = DateTime.MaxValue
            }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildWithMinimumActiveTime()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions
            {
                ValidFrom = DateTime.Now,
                ValidTo = DateTime.Now + TimeSpan.FromSeconds(30)
            }));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildWithoutAvailableEnvironments()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>
                            {
                                new StructureIdentifier("Foo", 42)
                            }))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>()))
                            .Verifiable("available environments not retrieved");

            var result = await TestAction<NotFoundObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildWithoutAvailableStructures()
        {
            _projectionStore.Setup(s => s.Structures.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>()))
                            .Verifiable("available structures not retrieved");

            _projectionStore.Setup(s => s.Environments.GetAvailable(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<EnvironmentIdentifier>>(new List<EnvironmentIdentifier>
                            {
                                new EnvironmentIdentifier("Foo", "Bar")
                            }))
                            .Verifiable("available environments not retrieved");

            var result = await TestAction<NotFoundObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, new ConfigurationBuildOptions()));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task BuildWithoutOptions()
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.BuildConfiguration("Foo", "Bar", "Foo", 42, null));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAvailable()
        {
            _projectionStore.Setup(s => s.Configurations.GetAvailable(It.IsAny<DateTime>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>
                            {
                                new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                            new StructureIdentifier("Foo", 42),
                                                            0)
                            }))
                            .Verifiable("available configurations not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetConfigurations(DateTime.Now));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<ConfigurationIdentifier>>(result.Value);
            Assert.NotEmpty((List<ConfigurationIdentifier>) result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAvailableProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetAvailable(It.IsAny<DateTime>(), It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<ConfigurationIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("available configurations not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfigurations(DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetAvailableStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetAvailable(It.IsAny<DateTime>(), It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("available configurations not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfigurations(DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfiguration()
        {
            _projectionStore.Setup(s => s.Configurations.GetKeys(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                             new StructureIdentifier("Foo", 42),
                                                                                             0),
                                                                 It.IsAny<DateTime>(),
                                                                 It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("configuration not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetVersion(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                                new StructureIdentifier("Foo", 42),
                                                                                                0),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Success("some version"))
                            .Verifiable("configuration-version not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetConfiguration("Foo", "Bar", "Foo", 42, DateTime.Now),
                                                          c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<Dictionary<string, string>>(result.Value);
            Assert.NotEmpty((Dictionary<string, string>) result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfigurationJson()
        {
            _projectionStore.Setup(s => s.Configurations.GetJson(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                             new StructureIdentifier("Foo", 42),
                                                                                             0),
                                                                 It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Success(JsonDocument.Parse("{\"Foo\":\"Bar\"}").RootElement))
                            .Verifiable("configuration not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetVersion(new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                                                                new StructureIdentifier("Foo", 42),
                                                                                                0),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Success("some version"))
                            .Verifiable("configuration-version not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetConfigurationJson("Foo", "Bar", "Foo", 42, DateTime.Now),
                                                          c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfigurationJsonProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetJson(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Error<JsonElement>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("configuration not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfigurationJson("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfigurationJsonStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetJson(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>()))
                            .Throws<Exception>()
                            .Verifiable("configuration not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfigurationJson("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetConfigurationJsonVersionProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetJson(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Success(JsonDocument.Parse("{\"Json\":\"Bar\"}").RootElement))
                            .Verifiable("configuration not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetVersion(It.IsAny<ConfigurationIdentifier>(),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Error<string>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("configuration-version not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfigurationJson("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfigurationProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>(),
                                                                 It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IDictionary<string, string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("configuration not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfiguration("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetConfigurationStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>(),
                                                                 It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("configuration not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfiguration("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetConfigurationVersionProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                 It.IsAny<DateTime>(),
                                                                 It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IDictionary<string, string>>(new Dictionary<string, string>
                            {
                                {"Foo", "Bar"}
                            }))
                            .Verifiable("configuration not retrieved");

            _projectionStore.Setup(s => s.Configurations.GetVersion(It.IsAny<ConfigurationIdentifier>(),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Error<string>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("configuration-version not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetConfiguration("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetStale()
        {
            _projectionStore.Setup(s => s.Configurations.GetStale(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IList<ConfigurationIdentifier>>(new List<ConfigurationIdentifier>
                            {
                                new ConfigurationIdentifier(new EnvironmentIdentifier("Foo", "Bar"),
                                                            new StructureIdentifier("Foo", 42),
                                                            0)
                            }))
                            .Verifiable("stale configurations not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetStaleConfigurations());

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<List<ConfigurationIdentifier>>(result.Value);
            Assert.NotEmpty((List<ConfigurationIdentifier>) result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetStaleProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetStale(It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IList<ConfigurationIdentifier>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("stale configurations not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStaleConfigurations());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetStaleStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetStale(It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("stale configurations not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetStaleConfigurations());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeys()
        {
            _projectionStore.Setup(s => s.Configurations.GetUsedConfigurationKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                                  It.IsAny<DateTime>(),
                                                                                  It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Success<IEnumerable<string>>(new[] {"Foo"}))
                            .Verifiable("used keys not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetUsedKeys("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            Assert.IsAssignableFrom<string[]>(result.Value);
            Assert.NotEmpty((string[]) result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetUsedConfigurationKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                                  It.IsAny<DateTime>(),
                                                                                  It.IsAny<QueryRange>()))
                            .ReturnsAsync(() => Result.Error<IEnumerable<string>>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("used keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeys("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetUsedKeysStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetUsedConfigurationKeys(It.IsAny<ConfigurationIdentifier>(),
                                                                                  It.IsAny<DateTime>(),
                                                                                  It.IsAny<QueryRange>()))
                            .Throws<Exception>()
                            .Verifiable("used keys not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetUsedKeys("Foo", "Bar", "Foo", 42, DateTime.Now));

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVersion()
        {
            _projectionStore.Setup(s => s.Configurations.GetVersion(It.IsAny<ConfigurationIdentifier>(),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Success("some version"))
                            .Verifiable("version not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetVersion("Foo", "Bar", "Foo", 42, DateTime.Now),
                                                          c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVersionProviderError()
        {
            _projectionStore.Setup(s => s.Configurations.GetVersion(It.IsAny<ConfigurationIdentifier>(),
                                                                    It.IsAny<DateTime>()))
                            .ReturnsAsync(() => Result.Error<string>("something went wrong", ErrorCode.DbQueryError))
                            .Verifiable("version not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetVersion("Foo", "Bar", "Foo", 42, DateTime.Now),
                                                        c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }

        [Fact]
        public async Task GetVersionStoreThrows()
        {
            _projectionStore.Setup(s => s.Configurations.GetVersion(It.IsAny<ConfigurationIdentifier>(),
                                                                    It.IsAny<DateTime>()))
                            .Throws<Exception>()
                            .Verifiable("version not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetVersion("Foo", "Bar", "Foo", 42, DateTime.Now),
                                                        c => c.ControllerContext.HttpContext = new DefaultHttpContext());

            Assert.NotNull(result.Value);
            _projectionStore.Verify();
        }
    }
}