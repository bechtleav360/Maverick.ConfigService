using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DbObjects;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests
{
    public class ConfigurationDatabaseTests : IDisposable
    {
        public ConfigurationDatabaseTests()
        {
            _sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder {DataSource = ":memory:"}.ToString());
            _sqliteConnection.Open();

            IServiceCollection services = new ServiceCollection();

            services.AddLogging();
            services.AddDbContext<ProjectionStoreContext>(
                builder => builder.UseSqlite(_sqliteConnection,
                                             o => o.MigrationsAssembly("Bechtle.A365.ConfigService.Migrations")));

            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<ConfigurationDatabase>>();

            _context = provider.GetRequiredService<ProjectionStoreContext>();
            _database = new ConfigurationDatabase(logger, _context);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _sqliteConnection?.Close();
            _sqliteConnection?.Dispose();
        }

        private readonly IConfigurationDatabase _database;
        private readonly ProjectionStoreContext _context;
        private readonly SqliteConnection _sqliteConnection;

        [Fact]
        public async Task AppendProjectedEventMetadata() => throw new NotImplementedException();

        [Fact]
        public async Task ApplyEnvironmentChanges() => throw new NotImplementedException();

        [Fact]
        public async Task ApplyStructureChanges() => throw new NotImplementedException();

        [Fact]
        public async Task Connect()
        {
            var result = await _database.Connect();

            Assert.NotNull(result);
            Assert.False(result.IsError);
        }

        [Fact]
        public async Task CreateEnvironment()
        {
            var result = await _database.CreateEnvironment(new EnvironmentIdentifier("Foo", "Bar"), false);

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Single(_context.ConfigEnvironments.Where(e => e.Category.Equals("Foo", StringComparison.Ordinal)
                                                                 && e.Name.Equals("Bar", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task CreateStructure()
        {
            var result = await _database.CreateStructure(new StructureIdentifier("Foo", 42),
                                                         new Dictionary<string, string> {{"Key1", "Value1"}},
                                                         new Dictionary<string, string> {{"Var1", "Val1"}});

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Single(_context.Structures.Where(s => s.Name.Equals("Foo", StringComparison.Ordinal)
                                                         && s.Version == 42));
        }

        [Fact]
        public async Task DeleteEnvironment() => throw new NotImplementedException();

        [Fact]
        public async Task DeleteStructure() => throw new NotImplementedException();

        [Fact]
        public async Task GenerateEnvironmentKeyAutocompleteData() => throw new NotImplementedException();

        [Fact]
        public async Task GetDefaultEnvironment() => throw new NotImplementedException();

        [Fact]
        public async Task GetEnvironment() => throw new NotImplementedException();

        [Fact]
        public async Task GetEnvironmentWithInheritance() => throw new NotImplementedException();

        [Fact]
        public async Task GetLatestActiveConfiguration() => throw new NotImplementedException();

        [Fact]
        public async Task GetLatestProjectedEventId() => throw new NotImplementedException();

        [Fact]
        public async Task GetProjectedEventMetadata() => throw new NotImplementedException();

        [Fact]
        public async Task GetStructure() => throw new NotImplementedException();

        [Fact]
        public async Task ImportEnvironment() => throw new NotImplementedException();

        [Fact]
        public async Task SaveConfiguration() => throw new NotImplementedException();

        [Fact]
        public async Task SetLatestActiveConfiguration() => throw new NotImplementedException();

        [Fact]
        public async Task SetLatestProjectedEventId() => throw new NotImplementedException();
    }
}