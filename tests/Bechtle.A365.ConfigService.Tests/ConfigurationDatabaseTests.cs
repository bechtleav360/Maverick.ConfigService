using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
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
        public async Task AppendProjectedEventMetadataSucceeds()
        {
            var result = await _database.AppendProjectedEventMetadata(new ProjectedEventMetadata
            {
                Id = Guid.NewGuid(),
                Type = "FooEventType",
                Index = 42,
                Changes = 123456,
                End = new DateTime(2, 2, 2, 2, 2, 2, DateTimeKind.Utc),
                ProjectedSuccessfully = true,
                Start = new DateTime(1, 1, 1, 1, 1, 1, DateTimeKind.Utc)
            });

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Single(_context.ProjectedEventMetadata.Where(m => m.Index == 42));
        }

        [Fact]
        public async Task AppendProjectedEventMetadataSavesRecordExactly()
        {
            var expected = new ProjectedEventMetadata
            {
                Id = Guid.Parse("{CA215812-2A48-432A-B01A-A70E85A33792}"),
                Type = "FooEventType",
                Index = 42,
                Changes = 123456,
                End = new DateTime(2, 2, 2, 2, 2, 2, DateTimeKind.Utc),
                ProjectedSuccessfully = true,
                Start = new DateTime(1, 1, 1, 1, 1, 1, DateTimeKind.Utc)
            };
            var result = await _database.AppendProjectedEventMetadata(expected);

            Assert.NotNull(result);
            Assert.False(result.IsError);

            var actual = _context.ProjectedEventMetadata.Single(m => m.Index == 42);

            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Changes, actual.Changes);
            Assert.Equal(expected.End, actual.End);
            Assert.Equal(expected.ProjectedSuccessfully, actual.ProjectedSuccessfully);
            Assert.Equal(expected.Start, actual.Start);
        }

        [Fact]
        public async Task ApplyEmptyChangesToEnvironment()
        {
            var initialKey = new ConfigEnvironmentKey
            {
                Id = Guid.Parse("{40F1B991-5D85-48B0-B609-288031D03D9A}"),
                Description = "Key1-Description",
                Key = "Key1",
                Type = "Key1-Type",
                Value = "Key1-Value",
                Version = 424242
            };

            var configEnvironment = new ConfigEnvironment
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Keys = new List<ConfigEnvironmentKey> {initialKey},
                Name = "Bar",
                Category = "Foo",
                DefaultEnvironment = false
            };

            await _context.ConfigEnvironments.AddAsync(configEnvironment);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new EnvironmentIdentifier("Foo", "Bar"), new List<ConfigKeyAction>());

            Assert.NotNull(result);
            Assert.False(result.IsError);

            Assert.Single(_context.ConfigEnvironments.Where(env => env.Id == configEnvironment.Id));
            Assert.Single(_context.ConfigEnvironmentKeys.Where(k => k.Id == initialKey.Id));
        }

        [Fact]
        public async Task ApplyChangesToEnvironment()
        {
            var initialKey = new ConfigEnvironmentKey
            {
                Id = Guid.Parse("{40F1B991-5D85-48B0-B609-288031D03D9A}"),
                Description = "Key1-Description",
                Key = "Key1",
                Type = "Key1-Type",
                Value = "Key1-Value",
                Version = 424242
            };

            var configEnvironment = new ConfigEnvironment
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Keys = new List<ConfigEnvironmentKey> {initialKey},
                Name = "Bar",
                Category = "Foo",
                DefaultEnvironment = false
            };

            await _context.ConfigEnvironments.AddAsync(configEnvironment);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new EnvironmentIdentifier("Foo", "Bar"),
                                                      new List<ConfigKeyAction>
                                                      {
                                                          ConfigKeyAction.Delete("Key1"),
                                                          ConfigKeyAction.Set("Key2", "Key2-Value", "Key2-Description", "Key2-Type")
                                                      });

            Assert.NotNull(result);
            Assert.False(result.IsError);

            Assert.Empty(_context.ConfigEnvironmentKeys.Where(k => k.Id == initialKey.Id));
            Assert.Single(_context.ConfigEnvironmentKeys.Where(k => k.Key.Equals("Key2", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task ApplyChangesWithUndefinedTarget()
        {
            var initialKey = new ConfigEnvironmentKey
            {
                Id = Guid.Parse("{40F1B991-5D85-48B0-B609-288031D03D9A}"),
                Description = "Key1-Description",
                Key = "Key1",
                Type = "Key1-Type",
                Value = "Key1-Value",
                Version = 424242
            };

            var configEnvironment = new ConfigEnvironment
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Keys = new List<ConfigEnvironmentKey> {initialKey},
                Name = "Bar",
                Category = "Foo",
                DefaultEnvironment = false
            };

            await _context.ConfigEnvironments.AddAsync(configEnvironment);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new EnvironmentIdentifier("Invalid", "Target"),
                                                      new List<ConfigKeyAction>
                                                      {
                                                          ConfigKeyAction.Delete("Key1"),
                                                          ConfigKeyAction.Set("Key2", "Key2-Value", "Key2-Description", "Key2-Type")
                                                      });

            Assert.NotNull(result);
            Assert.True(result.IsError);
        }

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
        public async Task CreateDefaultEnvironment()
        {
            var result = await _database.CreateEnvironment(new EnvironmentIdentifier("Foo", "Bar"), true);

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Single(_context.ConfigEnvironments.Where(e => e.Category.Equals("Foo", StringComparison.Ordinal)
                                                                 && e.Name.Equals("Bar", StringComparison.Ordinal)
                                                                 && e.DefaultEnvironment));
        }

        [Fact]
        public async Task CreateEnvironment()
        {
            var result = await _database.CreateEnvironment(new EnvironmentIdentifier("Foo", "Bar"), false);

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Single(_context.ConfigEnvironments.Where(e => e.Category.Equals("Foo", StringComparison.Ordinal)
                                                                 && e.Name.Equals("Bar", StringComparison.Ordinal)
                                                                 && e.DefaultEnvironment == false));
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