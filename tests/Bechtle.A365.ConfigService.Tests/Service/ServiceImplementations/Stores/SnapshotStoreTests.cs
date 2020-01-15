using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public abstract class SnapshotStoreTests
    {
        protected ISnapshotStore Store { get; set; }

        [Fact]
        public async Task RetrieveDomainObjectSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            item.Create();
            item.ImportKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Jar", "Jar", "", "", 4711)});

            var snapshot = item.CreateSnapshot();

            await Store.SaveSnapshots(new[] {snapshot});

            var result = await Store.GetSnapshot<ConfigEnvironment>(new EnvironmentIdentifier("Foo", "Bar").ToString());

            Assert.False(result.IsError);
            Assert.NotNull(result.Data);
            Assert.Equal(snapshot, result.Data);
        }

        [Fact]
        public async Task RetrieveGenericExistingSnapshot()
        {
            var snapshot = new DomainObjectSnapshot("UT-Type", "UT-Id", "{\"UT-Data\":4711}", 1, 2);

            await Store.SaveSnapshots(new[] {snapshot});

            var result = await Store.GetSnapshot("UT-Type", "UT-Id");

            Assert.False(result.IsError, "result.IsError");
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task StoreDomainObjectSnapshot()
        {
            var item = new ConfigEnvironment(new EnvironmentIdentifier("Foo", "Bar"));
            item.Create();
            item.ImportKeys(new List<ConfigEnvironmentKey> {new ConfigEnvironmentKey("Jar", "Jar", "", "", 4711)});

            var snapshot = item.CreateSnapshot();

            var result = await Store.SaveSnapshots(new[] {snapshot});

            Assert.False(result.IsError, "result.IsError");
        }

        [Fact]
        public async Task StoreGenericSnapshot()
        {
            var snapshot = new DomainObjectSnapshot("UT-Type", "UT-Id", "{\"UT-Data\":4711}", 1, 2);

            var result = await Store.SaveSnapshots(new[] {snapshot});

            Assert.False(result.IsError, "result.IsError");
        }
    }
}