using Bechtle.A365.ConfigService.Implementations.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class MemorySnapshotStoreTests : SnapshotStoreTests
    {
        /// <inheritdoc />
        public MemorySnapshotStoreTests()
        {
            var provider = new ServiceCollection().AddLogging().BuildServiceProvider();

            var instance = new MemorySnapshotStore(provider.GetRequiredService<ILogger<MemorySnapshotStore>>());
            instance.Clear();

            Store = instance;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }
    }
}