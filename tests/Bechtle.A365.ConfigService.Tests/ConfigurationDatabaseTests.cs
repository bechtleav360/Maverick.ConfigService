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
        public async Task ApplyEnvironmentChangesWithUndefinedTarget()
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
        public async Task ApplyEmptyChangesToStructure()
        {
            var initialKey = new StructureKey
            {
                Id = Guid.Parse("{2F6C004A-13EE-472C-AACC-F2B1F25ACFD8}"),
                Key = "Key1",
                Value = "Value1"
            };

            var initialVariable = new StructureVariable
            {
                Id = Guid.Parse("{9FB568E8-D5F7-49C9-8425-DA9D4BBF3510}"),
                Key = "Var1",
                Value = "Val1"
            };

            var structure = new Structure
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Name = "Foo",
                Version = 42,
                Keys = new List<StructureKey> {initialKey},
                Variables = new List<StructureVariable> {initialVariable}
            };

            await _context.Structures.AddAsync(structure);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new StructureIdentifier("Foo", 42), new List<ConfigKeyAction>());

            Assert.NotNull(result);
            Assert.False(result.IsError);

            Assert.Single(_context.Structures.Where(s => s.Id == structure.Id));
            Assert.Single(_context.StructureKeys.Where(k => k.Id == initialKey.Id));
            Assert.Single(_context.StructureVariables.Where(v => v.Id == initialVariable.Id));
        }

        [Fact]
        public async Task ApplyChangesToStructure()
        {
            var initialKey = new StructureKey
            {
                Id = Guid.Parse("{2F6C004A-13EE-472C-AACC-F2B1F25ACFD8}"),
                Key = "Key1",
                Value = "Value1"
            };

            var initialVariable = new StructureVariable
            {
                Id = Guid.Parse("{9FB568E8-D5F7-49C9-8425-DA9D4BBF3510}"),
                Key = "Var1",
                Value = "Val1"
            };

            var structure = new Structure
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Name = "Foo",
                Version = 42,
                Keys = new List<StructureKey> {initialKey},
                Variables = new List<StructureVariable> {initialVariable}
            };

            await _context.Structures.AddAsync(structure);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new StructureIdentifier("Foo", 42), new List<ConfigKeyAction>
            {
                ConfigKeyAction.Delete("Var1"),
                ConfigKeyAction.Set("Var2", "Val2", null, null)
            });

            Assert.NotNull(result);
            Assert.False(result.IsError);

            Assert.Single(_context.Structures.Where(s => s.Id == structure.Id));
            Assert.Single(_context.StructureKeys.Where(k => k.Id == initialKey.Id));
            Assert.Empty(_context.StructureVariables.Where(v => v.Id == initialVariable.Id));
            Assert.Single(_context.StructureVariables.Where(v => v.Key.Equals("Var2", StringComparison.Ordinal)
                                                                 && v.Value.Equals("Val2", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task ApplyStructureChangesWithUndefinedTarget()
        {
            var initialKey = new StructureKey
            {
                Id = Guid.Parse("{2F6C004A-13EE-472C-AACC-F2B1F25ACFD8}"),
                Key = "Key1",
                Value = "Value1"
            };

            var initialVariable = new StructureVariable
            {
                Id = Guid.Parse("{9FB568E8-D5F7-49C9-8425-DA9D4BBF3510}"),
                Key = "Var1",
                Value = "Val1"
            };

            var structure = new Structure
            {
                Id = Guid.Parse("{973D5AF0-0368-4BA4-8D75-3D166A0E15E8}"),
                Name = "Foo",
                Version = 42,
                Keys = new List<StructureKey> {initialKey},
                Variables = new List<StructureVariable> {initialVariable}
            };

            await _context.Structures.AddAsync(structure);
            await _context.SaveChangesAsync();

            // test
            var result = await _database.ApplyChanges(new StructureIdentifier("InvalidStructure", 41), new List<ConfigKeyAction>
            {
                ConfigKeyAction.Delete("Key1"),
                ConfigKeyAction.Set("Key2", "Value2", "Description2", "Value2")
            });

            Assert.NotNull(result);
            Assert.True(result.IsError);

            Assert.Single(_context.Structures.Where(s => s.Id == structure.Id));
            Assert.Single(_context.StructureKeys.Where(k => k.Id == initialKey.Id));
            Assert.Single(_context.StructureVariables.Where(v => v.Id == initialVariable.Id));
        }

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
        public async Task DeleteEnvironment()
        {
            var configEnvironment = new ConfigEnvironment
            {
                Id = Guid.Parse("{BE1C159C-E132-4DD1-A412-8EF8CED3E44C}"),
                Category = "Foo",
                Name = "Bar",
                DefaultEnvironment = false,
                Keys = new List<ConfigEnvironmentKey>
                {
                    new ConfigEnvironmentKey
                    {
                        Id = Guid.Parse("{044D97B2-97EC-4180-AA77-46B56BFC5BED}"),
                        Key = "Key1",
                        Value = "Value",
                        Description = "Description1",
                        Type = "Type1",
                        Version = 424242
                    }
                }
            };

            await _context.ConfigEnvironments.AddAsync(configEnvironment);
            await _context.SaveChangesAsync();

            var result = await _database.DeleteEnvironment(new EnvironmentIdentifier(configEnvironment.Category, configEnvironment.Name));

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Empty(_context.ConfigEnvironments);
        }

        [Fact]
        public async Task DeleteStructure()
        {
            var structure = new Structure
            {
                Id = Guid.Parse("{CD504D98-AEDF-415E-8EEB-FBBB20814348}"),
                Name = "Foo",
                Version = 42,
                Keys = new List<StructureKey>(),
                Variables = new List<StructureVariable>()
            };

            await _context.Structures.AddAsync(structure);
            await _context.SaveChangesAsync();

            var result = await _database.DeleteStructure(new StructureIdentifier(structure.Name, structure.Version));

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Empty(_context.Structures);
        }

        [Fact]
        public async Task DeleteStructureWithKeysAndVariables()
        {
            var structure = new Structure
            {
                Id = Guid.Parse("{CD504D98-AEDF-415E-8EEB-FBBB20814348}"),
                Name = "Foo",
                Version = 42,
                Keys = new List<StructureKey>
                {
                    new StructureKey
                    {
                        Id = Guid.Parse("{8AC5EBA8-AD60-407C-90A1-A0E207A3D967}"),
                        Key = "Key1",
                        Value = "Value1"
                    }
                },
                Variables = new List<StructureVariable>
                {
                    new StructureVariable
                    {
                        Id = Guid.Parse("{E367214C-B1C0-4DB2-AD0D-FB40E2C9E536}"),
                        Key = "Var1",
                        Value = "Val1"
                    }
                }
            };

            await _context.Structures.AddAsync(structure);
            await _context.SaveChangesAsync();

            var result = await _database.DeleteStructure(new StructureIdentifier(structure.Name, structure.Version));

            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.Empty(_context.Structures);
            Assert.Empty(_context.StructureKeys);
            Assert.Empty(_context.StructureVariables);
        }

        [Fact]
        public async Task GenerateEnvironmentKeyAutocompleteData() => throw new NotImplementedException();

        [Fact]
        public async Task GetDefaultEnvironmentWithExpectedName()
        {
            var expected = new ConfigEnvironment
            {
                Id = Guid.Parse("{F02C0718-AEC3-4417-BF3A-E02DC6023A73}"),
                Category = "Foo",
                Name = "Default",
                DefaultEnvironment = true,
                Keys = new List<ConfigEnvironmentKey>
                {
                    new ConfigEnvironmentKey
                    {
                        Id = Guid.Parse("{D1538E9C-5FB1-4C75-8AEA-7D607A2C3AC3}"),
                        Key = "Key1",
                        Value = "Value",
                        Description = "Description1",
                        Type = "Type1",
                        Version = 424242
                    }
                }
            };

            var diversion = new[]
            {
                new ConfigEnvironment
                {
                    Id = Guid.Parse("{0674EFD4-8188-406D-9488-5B184074D3D6}"),
                    Category = "Foo",
                    Name = "Bar",
                    DefaultEnvironment = false,
                    Keys = new List<ConfigEnvironmentKey>
                    {
                        new ConfigEnvironmentKey
                        {
                            Id = Guid.Parse("{48EA9B43-C62B-43E8-9FDF-2AB6678ECF4C}"),
                            Key = "Key1",
                            Value = "Value",
                            Description = "Description1",
                            Type = "Type1",
                            Version = 424242
                        }
                    }
                },
                new ConfigEnvironment
                {
                    Id = Guid.Parse("{54478C97-F597-4226-8CA2-3D62171BB208}"),
                    Category = "Foo",
                    Name = "Baz",
                    DefaultEnvironment = false,
                    Keys = new List<ConfigEnvironmentKey>
                    {
                        new ConfigEnvironmentKey
                        {
                            Id = Guid.Parse("{06AF4A9C-7368-4A99-B2AE-FA61727FB22C}"),
                            Key = "Key1",
                            Value = "Value",
                            Description = "Description1",
                            Type = "Type1",
                            Version = 424242
                        }
                    }
                }
            };

            await _context.ConfigEnvironments.AddAsync(expected);
            await _context.ConfigEnvironments.AddRangeAsync(diversion);
            await _context.SaveChangesAsync();

            var result = await _database.GetDefaultEnvironment("Foo");

            Assert.Equal(expected.Category, result.Data.Identifier.Category);
            Assert.Equal(expected.Name, result.Data.Identifier.Name);
        }

        [Fact]
        public async Task GetDefaultEnvironmentWithCustomName()
        {
            var expected = new ConfigEnvironment
            {
                Id = Guid.Parse("{F02C0718-AEC3-4417-BF3A-E02DC6023A73}"),
                Category = "Foo",
                Name = "Def",
                DefaultEnvironment = true,
                Keys = new List<ConfigEnvironmentKey>
                {
                    new ConfigEnvironmentKey
                    {
                        Id = Guid.Parse("{D1538E9C-5FB1-4C75-8AEA-7D607A2C3AC3}"),
                        Key = "Key1",
                        Value = "Value",
                        Description = "Description1",
                        Type = "Type1",
                        Version = 424242
                    }
                }
            };

            var diversion = new[]
            {
                new ConfigEnvironment
                {
                    Id = Guid.Parse("{0674EFD4-8188-406D-9488-5B184074D3D6}"),
                    Category = "Foo",
                    Name = "Bar",
                    DefaultEnvironment = false,
                    Keys = new List<ConfigEnvironmentKey>
                    {
                        new ConfigEnvironmentKey
                        {
                            Id = Guid.Parse("{48EA9B43-C62B-43E8-9FDF-2AB6678ECF4C}"),
                            Key = "Key1",
                            Value = "Value",
                            Description = "Description1",
                            Type = "Type1",
                            Version = 424242
                        }
                    }
                },
                new ConfigEnvironment
                {
                    Id = Guid.Parse("{54478C97-F597-4226-8CA2-3D62171BB208}"),
                    Category = "Foo",
                    Name = "Baz",
                    DefaultEnvironment = false,
                    Keys = new List<ConfigEnvironmentKey>
                    {
                        new ConfigEnvironmentKey
                        {
                            Id = Guid.Parse("{06AF4A9C-7368-4A99-B2AE-FA61727FB22C}"),
                            Key = "Key1",
                            Value = "Value",
                            Description = "Description1",
                            Type = "Type1",
                            Version = 424242
                        }
                    }
                }
            };

            await _context.ConfigEnvironments.AddAsync(expected);
            await _context.ConfigEnvironments.AddRangeAsync(diversion);
            await _context.SaveChangesAsync();

            var result = await _database.GetDefaultEnvironment("Foo");

            Assert.Equal(expected.Category, result.Data.Identifier.Category);
            Assert.Equal(expected.Name, result.Data.Identifier.Name);
        }

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