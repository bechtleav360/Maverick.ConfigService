using System;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public sealed class MsSqlSnapshotStoreTests : SnapshotStoreTests
    {
        /// <inheritdoc />
        public MsSqlSnapshotStoreTests()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddDbContext<MsSqlSnapshotStore.MsSqlSnapshotContext>(
                               builder => builder.EnableDetailedErrors()
                                                 .EnableSensitiveDataLogging()
                                                 .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                 .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)))
                           .BuildServiceProvider();

            Store = new MsSqlSnapshotStore(provider.GetRequiredService<ILogger<MsSqlSnapshotStore>>(),
                                           provider.GetRequiredService<MsSqlSnapshotStore.MsSqlSnapshotContext>());
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }
    }
}