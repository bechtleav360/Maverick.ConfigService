using System;
using System.IO;
using Bechtle.A365.ConfigService.Common.DbContexts;
using Bechtle.A365.ConfigService.Implementations.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Tests.Service.ServiceImplementations.Stores
{
    public class LocalFileSnapshotStoreTests : SnapshotStoreTests
    {
        private readonly string _databaseName;

        /// <inheritdoc />
        public LocalFileSnapshotStoreTests()
        {
            _databaseName = $"{Guid.NewGuid():N}.db";

            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = _databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var provider = new ServiceCollection()
                           .AddLogging()
                           .AddDbContext<SqliteSnapshotContext>(
                               builder => builder.EnableDetailedErrors()
                                                 .EnableSensitiveDataLogging()
                                                 .UseSqlite(connectionStringBuilder.ToString()))
                           .BuildServiceProvider();

            Store = new LocalFileSnapshotStore(provider.GetRequiredService<ILogger<LocalFileSnapshotStore>>(),
                                               provider.GetRequiredService<SqliteSnapshotContext>());
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            try
            {
                File.Delete(_databaseName);
            }
            catch (IOException)
            {
                // ¯\_(ツ)_/¯
            }
        }
    }
}