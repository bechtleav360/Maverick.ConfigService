using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class ConfigurationProjectionStoreTests
    {
        private (ILogger<ConfigurationProjectionStore> logger, Mock<IDomainObjectManager> DomainObjectManager) CreateMocks()
            => (new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
                .GetRequiredService<ILogger<ConfigurationProjectionStore>>(),
                   new Mock<IDomainObjectManager>(MockBehavior.Strict));

        private void VerifySetups(params Mock[] mocks)
        {
            foreach (var mock in mocks)
                mock.Verify();
        }

        private ConfigurationIdentifier CreateConfigurationIdentifier(
            string envCategory = "Foo",
            string envName = "Bar",
            string structName = "Foo",
            int structVersion = 42,
            long version = 4711)
            => new ConfigurationIdentifier(
                new EnvironmentIdentifier(envCategory, envName),
                new StructureIdentifier(structName, structVersion),
                version);

        [Fact]
        public async Task BuildNewConfig()
        {
            var (logger, domainObjectManager) = CreateMocks();

            domainObjectManager.Setup(
                                   m => m.CreateConfiguration(
                                       It.IsAny<ConfigurationIdentifier>(),
                                       null,
                                       null,
                                       It.IsAny<CancellationToken>()))
                               .ReturnsAsync(Result.Success())
                               .Verifiable("Manager was not called");

            var store = new ConfigurationProjectionStore(logger, domainObjectManager.Object);

            var result = await store.Build(
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
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailable(DateTime.UtcNow, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAllAvailablePaged()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailable(DateTime.UtcNow, QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentEmpty()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailableWithEnvironment(
                             new EnvironmentIdentifier("Foo", "Bar"),
                             DateTime.UtcNow,
                             QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAvailableForEnvironmentPaged()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailableWithEnvironment(
                             new EnvironmentIdentifier("Foo", "Bar"),
                             DateTime.UtcNow,
                             QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetAvailableForStructureEmpty()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailableWithStructure(
                             new StructureIdentifier("Imaginary", 42),
                             DateTime.UtcNow,
                             QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAvailableForStructurePaged()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetAvailableWithEnvironment(
                             new EnvironmentIdentifier("Foo", "Bar"),
                             DateTime.UtcNow,
                             QueryRange.Make(1, 1));

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetConfigVersion()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetVersion(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task GetJson()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetJson(CreateConfigurationIdentifier(), DateTime.Now);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data.GetProperty("valid").GetBoolean(), "result.Data.GetProperty('valid').GetBoolean()");
        }

        [Fact]
        public async Task GetKeys()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task GetStale()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetStale(QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetUsedKeys()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.GetUsedConfigurationKeys(CreateConfigurationIdentifier(), DateTime.Now, QueryRange.All);

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task IsNotStale()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.False(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStale()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }

        [Fact]
        public async Task IsStaleUnknown()
        {
            var (logger, domainObjectManager) = CreateMocks();

            var store = new ConfigurationProjectionStore(
                logger,
                domainObjectManager.Object);

            var result = await store.IsStale(CreateConfigurationIdentifier());

            VerifySetups(domainObjectManager);

            Assert.Empty(result.Message);
            Assert.False(result.IsError, "result.IsError");
            Assert.True(result.Data, "result.Data");
        }
    }
}
