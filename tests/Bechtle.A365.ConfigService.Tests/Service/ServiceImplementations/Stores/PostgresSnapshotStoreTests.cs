using System;
using Bechtle.A365.ConfigService.Common.DbContexts;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class PostgresSnapshotStoreTests : SnapshotStoreTests
    {
        /// <inheritdoc />
        public PostgresSnapshotStoreTests()
        {
            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddDbContext<SnapshotContext>(
                               builder => builder.EnableDetailedErrors()
                                                 .EnableSensitiveDataLogging()
                                                 .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                 .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)))
                           .BuildServiceProvider();

            Store = new PostgresSnapshotStore(provider.GetRequiredService<ILogger<PostgresSnapshotStore>>(),
                                              provider.GetRequiredService<SnapshotContext>());
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }
    }
}