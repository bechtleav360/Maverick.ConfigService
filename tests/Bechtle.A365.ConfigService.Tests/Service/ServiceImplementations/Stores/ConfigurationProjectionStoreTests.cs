using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class ConfigurationProjectionStoreTests
    {
        [Fact]
        public async Task BuildNewConfig()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.CreateConfiguration(
                                       It.IsAny<ConfigurationIdentifier>(),
                                       null,
                                       null,
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success())
                               .Verifiable("Manager was not called");

            var store = new ConfigurationProjectionStore(logger, domainObjectManager.Object);

            IResult result = await store.Build(
                                 new ConfigurationIdentifier(
                                     new EnvironmentIdentifier("Foo", "Bar"),
                                     new StructureIdentifier("Foo", 42),
                                     4711),
                                 null,
                                 null);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public async Task GetAllAvailableEmpty()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfigurations(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>()))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailable(DateTime.UtcNow, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetAllAvailablePaged()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfigurations(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>(new[] { CreateConfigurationIdentifier() })))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailable(DateTime.UtcNow, QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentEmpty()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.GetConfigurations(
                                       It.IsAny<EnvironmentIdentifier>(),
                                       It.IsAny<QueryRange>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>()))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailableWithEnvironment(
                                                                new EnvironmentIdentifier("Foo", "Bar"),
                                                                DateTime.UtcNow,
                                                                QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentPaged()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.GetConfigurations(
                                       It.IsAny<EnvironmentIdentifier>(),
                                       It.IsAny<QueryRange>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   Result.Success(new Page<ConfigurationIdentifier>(new[] { CreateConfigurationIdentifier() })))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailableWithEnvironment(
                                                                new EnvironmentIdentifier("Foo", "Bar"),
                                                                DateTime.UtcNow,
                                                                QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetAvailableForStructureEmpty()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.GetConfigurations(
                                       It.IsAny<StructureIdentifier>(),
                                       It.IsAny<QueryRange>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>()))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailableWithStructure(
                                                                new StructureIdentifier("Imaginary", 42),
                                                                DateTime.UtcNow,
                                                                QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetAvailableForStructurePaged()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.GetConfigurations(
                                       It.IsAny<StructureIdentifier>(),
                                       It.IsAny<QueryRange>(),
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>(new[] { CreateConfigurationIdentifier() })))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetAvailableWithStructure(
                                                                new StructureIdentifier("Foo", 1),
                                                                DateTime.UtcNow,
                                                                QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetConfigVersion()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfiguration(It.IsAny<ConfigurationIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync((ConfigurationIdentifier id, CancellationToken _) => Result.Success(new PreparedConfiguration(id)))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<string> result = await store.GetVersion(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData);
        }

        [Fact]
        public async Task GetJson()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfiguration(It.IsAny<ConfigurationIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (ConfigurationIdentifier id, CancellationToken _) => Result.Success(
                                       new PreparedConfiguration(id)
                                       {
                                           Json = "{\"valid\":true}"
                                       }))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<JsonElement> result = await store.GetJson(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data.GetProperty("valid").GetBoolean(), "result.Data.GetProperty('valid').GetBoolean()");
        }

        [Fact]
        public async Task GetKeys()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfiguration(It.IsAny<ConfigurationIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (ConfigurationIdentifier id, CancellationToken _) => Result.Success(
                                       new PreparedConfiguration(id)
                                       {
                                           Keys = new Dictionary<string, string?>
                                           {
                                               { "Foo", "Bar" }
                                           }
                                       }))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<KeyValuePair<string, string?>>> result = await store.GetKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetStale()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetStaleConfigurations(It.IsAny<QueryRange>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success(new Page<ConfigurationIdentifier>(new[] { CreateConfigurationIdentifier() })))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<ConfigurationIdentifier>> result = await store.GetStale(QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.CheckedData.Items);
        }

        [Fact]
        public async Task GetUsedKeys()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.GetConfiguration(It.IsAny<ConfigurationIdentifier>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(
                                   (ConfigurationIdentifier id, CancellationToken _) =>
                                       Result.Success(new PreparedConfiguration(id) { UsedKeys = new List<string> { "Foo" } }))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<Page<string>> result = await store.GetUsedConfigurationKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.CheckedData.Items);
        }

        [Fact]
        public async Task IsNotStale()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.IsStale(It.IsAny<ConfigurationIdentifier>()))
                               .ReturnsAsync(Result.Success(false))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<bool> result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.False(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStale()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.IsStale(It.IsAny<ConfigurationIdentifier>()))
                               .ReturnsAsync(Result.Success(true))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<bool> result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStaleUnknown()
        {
            (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(m => m.IsStale(It.IsAny<ConfigurationIdentifier>()))
                               .ReturnsAsync(Result.Success(true))
                               .Verifiable("DomainObjectManager was not queried for Configs");

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            IResult<bool> result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }

        private ConfigurationIdentifier CreateConfigurationIdentifier(
            string envCategory = "Foo",
            string envName = "Bar",
            string structName = "Foo",
            int structVersion = 42,
            long version = 4711)
            => new(
                new EnvironmentIdentifier(envCategory, envName),
                new StructureIdentifier(structName, structVersion),
                version);

        private (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> DomainObjectManager) CreateMocks()
            => (new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
                .GetRequiredService<ILogger<ConfigurationProjectionStore>>(),
                   new Mock<IDomainObjectManager>(MockBehavior.Strict));

        private void VerifySetups(params Mock[] mocks)
        {
            foreach (Mock mock in mocks)
            {
                mock.Verify();
            }
        }
    }
}
