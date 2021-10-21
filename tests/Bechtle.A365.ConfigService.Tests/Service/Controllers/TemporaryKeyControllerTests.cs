using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Controllers.V1;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Bechtle.A365.Core.EventBus.Abstraction;
using Bechtle.A365.Core.EventBus.Events.Messages;
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
        private readonly Mock<IEventBus> _eventBus = new(MockBehavior.Strict);
        private readonly Mock<ITemporaryKeyStore> _keyStore = new(MockBehavior.Strict);

        [Fact]
        public async Task Get()
        {
            _keyStore.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(() => Result.Success<string?>("Foo"))
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.Get("Foo", 42, "Bar"));

            Assert.Equal("Foo", result.Value);
            _keyStore.Verify();
        }

        [Fact]
        public async Task GetAll()
        {
            _keyStore.Setup(s => s.GetAll(It.IsAny<string>()))
                     .ReturnsAsync(
                         () => Result.Success<IDictionary<string, string?>>(
                             new Dictionary<string, string?>
                             {
                                 { "Foo", "Bar" }
                             }))
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetAll("Foo", 42));

            Assert.IsAssignableFrom<Dictionary<string, string>>(result.Value);
            Assert.NotEmpty((Dictionary<string, string>)result.Value);
            _keyStore.Verify();
        }

        [Theory]
        [InlineData("Foo", -1)]
        [InlineData("", 42)]
        [InlineData(null, 42)]
        public async Task GetAllInvalidParameters(string structure, int version)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.GetAll(structure, version));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetAllRegionNotFound()
        {
            _keyStore.Setup(s => s.GetAll(It.IsAny<string>()))
                     .ReturnsAsync(() => Result.Error<IDictionary<string, string?>>("something went wrong", ErrorCode.NotFound))
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<OkObjectResult>(c => c.GetAll("Foo", 42));

            Assert.IsAssignableFrom<Dictionary<string, string>>(result.Value);
            Assert.Empty((Dictionary<string, string>)result.Value);
            _keyStore.Verify();
        }

        [Fact]
        public async Task GetAllStoreThrows()
        {
            _keyStore.Setup(s => s.GetAll(It.IsAny<string>()))
                     .Throws<Exception>()
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<ObjectResult>(c => c.GetAll("Foo", 42));

            Assert.NotNull(result.Value);
            _keyStore.Verify();
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
        public async Task GetStoreThrows()
        {
            _keyStore.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>()))
                     .Throws<Exception>()
                     .Verifiable("temporary key not retrieved");

            var result = await TestAction<ObjectResult>(c => c.Get("Foo", 42, "Bar"));

            Assert.NotNull(result.Value);
            _keyStore.Verify();
        }

        [Fact]
        public async Task Refresh()
        {
            _keyStore.Setup(s => s.Extend(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(Result.Success)
                     .Verifiable("keys not extended");

            var result = await TestAction<ObjectResult>(
                             c => c.Set(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RefreshDefaultDuration()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.Refresh(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = default,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RefreshEmptyList()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.Refresh(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = Array.Empty<TemporaryKey>()
                                 }));

            Assert.NotNull(result.Value);
        }

        [Theory]
        [InlineData("Foo", -1)]
        [InlineData("", 42)]
        [InlineData(null, 42)]
        [InlineData("Foo", 42)]
        public async Task RefreshInvalidParameters(string structure, int version)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.Refresh(structure, version, null));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RefreshProviderError()
        {
            _keyStore.Setup(s => s.Extend(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                     .Verifiable("keys not extended");

            var result = await TestAction<ObjectResult>(
                             c => c.Refresh(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RefreshStoreThrows()
        {
            _keyStore.Setup(s => s.Extend(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<TimeSpan>()))
                     .Throws<Exception>()
                     .Verifiable("keys not extended");

            var result = await TestAction<ObjectResult>(
                             c => c.Refresh(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Remove()
        {
            _keyStore.Setup(s => s.Remove(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                     .ReturnsAsync(Result.Success)
                     .Verifiable("key not removed");

            _eventBus.Setup(e => e.Connect())
                     .Returns(Task.CompletedTask)
                     .Verifiable("EventBus not connected");

            _eventBus.Setup(e => e.Publish(It.IsAny<EventMessage>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable("remove-event not sent");

            await TestAction<OkResult>(c => c.Remove("Foo", 42, new[] { "Bar", "Baz" }));

            _keyStore.Verify();
            _eventBus.Verify();
        }

        [Fact]
        public async Task RemoveEmptyKeys()
        {
            await TestAction<BadRequestObjectResult>(c => c.Remove("Foo", 42, Array.Empty<string>()));
        }

        [Theory]
        [InlineData("Foo", -1)]
        [InlineData("", 42)]
        [InlineData(null, 42)]
        public async Task RemoveInvalidParameters(string structure, int version)
        {
            await TestAction<BadRequestObjectResult>(c => c.Remove(structure, version, new[] { "Foo" }));
        }

        [Fact]
        public async Task RemoveNullKeys()
        {
            await TestAction<BadRequestObjectResult>(c => c.Remove("Foo", 42, null));
        }

        [Fact]
        public async Task RemoveProviderError()
        {
            _keyStore.Setup(s => s.Remove(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                     .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                     .Verifiable("keys not removed");

            var result = await TestAction<ObjectResult>(c => c.Remove("Foo", 42, new[] { "Foo" }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RemoveStoreThrows()
        {
            _keyStore.Setup(s => s.Remove(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                     .Throws<Exception>()
                     .Verifiable("keys not removed");

            var result = await TestAction<ObjectResult>(c => c.Remove("Foo", 42, new[] { "Foo" }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task Set()
        {
            _keyStore.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<IDictionary<string, string?>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(Result.Success)
                     .Verifiable("keys not set");

            _eventBus.Setup(e => e.Connect())
                     .Returns(Task.CompletedTask)
                     .Verifiable("EventBus not connected");

            _eventBus.Setup(e => e.Publish(It.IsAny<EventMessage>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable("no event published");

            await TestAction<OkResult>(
                c => c.Set(
                    "Foo",
                    42,
                    new TemporaryKeyList
                    {
                        Duration = TimeSpan.FromMinutes(2),
                        Entries = new[]
                        {
                            new TemporaryKey
                            {
                                Key = "Foo",
                                Value = "Bar"
                            }
                        }
                    }));

            _keyStore.Verify();
        }

        [Fact]
        public async Task SetDefaultDuration()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.Set(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = default,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task SetEmptyList()
        {
            var result = await TestAction<BadRequestObjectResult>(
                             c => c.Set(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = Array.Empty<TemporaryKey>()
                                 }));

            Assert.NotNull(result.Value);
        }

        [Theory]
        [InlineData("Foo", -1)]
        [InlineData("", 42)]
        [InlineData(null, 42)]
        [InlineData("Foo", 42)]
        public async Task SetInvalidParameters(string structure, int version)
        {
            var result = await TestAction<BadRequestObjectResult>(c => c.Set(structure, version, null));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task SetProviderError()
        {
            _keyStore.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<IDictionary<string, string?>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(() => Result.Error("something went wrong", ErrorCode.DbUpdateError))
                     .Verifiable("keys not set");

            var result = await TestAction<ObjectResult>(
                             c => c.Set(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task SetStoreThrows()
        {
            _keyStore.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<IDictionary<string, string?>>(), It.IsAny<TimeSpan>()))
                     .Throws<Exception>()
                     .Verifiable("keys not set");

            var result = await TestAction<ObjectResult>(
                             c => c.Set(
                                 "Foo",
                                 42,
                                 new TemporaryKeyList
                                 {
                                     Duration = TimeSpan.MaxValue,
                                     Entries = new[]
                                     {
                                         new TemporaryKey
                                         {
                                             Key = "Foo",
                                             Value = "Bar"
                                         }
                                     }
                                 }));

            Assert.NotNull(result);
        }

        /// <inheritdoc />
        protected override TemporaryKeyController CreateController()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                                         .Build();

            ServiceProvider provider = new ServiceCollection().AddLogging()
                                                              .AddSingleton<IConfiguration>(configuration)
                                                              .BuildServiceProvider();

            return new TemporaryKeyController(
                provider.GetService<ILogger<TemporaryKeyController>>(),
                _keyStore.Object,
                _eventBus.Object);
        }
    }
}
