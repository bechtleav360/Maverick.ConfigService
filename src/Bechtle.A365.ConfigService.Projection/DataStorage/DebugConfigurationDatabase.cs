using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Dto;
using Bechtle.A365.ConfigService.Dto.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DataStorage
{
    public class DebugConfigurationDatabase : IConfigurationDatabase, IDisposable
    {
        private readonly Context _context;
        private readonly ILogger<DebugConfigurationDatabase> _logger;

        public DebugConfigurationDatabase(ILogger<DebugConfigurationDatabase> logger)
        {
            _logger = logger;
            _context = new Context();
        }

        /// <inheritdoc />
        public async Task<Result> ApplyChanges(EnvironmentIdentifier identifier, List<ConfigKeyAction> actions)
        {
            var environment = await _context.Environments.FirstOrDefaultAsync(env => env == identifier);

            if (environment is null)
            {
                _logger.LogError($"could not find environment {identifier} to apply modifications");
                return Result.Error($"could not find environment {identifier} to apply modifications", ErrorCode.NotFound);
            }

            foreach (var action in actions)
            {
                switch (action.Type)
                {
                    case ConfigKeyActionType.Set:
                    {
                        var existingKey = environment.Keys.FirstOrDefault(k => k.Key == action.Key);

                        if (existingKey is null)
                        {
                            environment.Keys.Add(new EnvironmentKey
                            {
                                Id = Guid.NewGuid(),
                                EnvironmentId = environment.Id,
                                Key = action.Key,
                                Value = action.Value,
                            });
                        }
                        else
                        {
                            existingKey.Value = action.Value;
                        }

                        break;
                    }

                    case ConfigKeyActionType.Delete:
                    {
                        var existingKey = environment.Keys.FirstOrDefault(k => k.Key == action.Key);
                        if (existingKey is null)
                        {
                            _logger.LogError($"could not remove key '{action.Key}' from environment {identifier}: not found");
                            return Result.Error($"could not remove key '{action.Key}' from environment {identifier}", ErrorCode.NotFound);
                        }

                        environment.Keys.Remove(existingKey);
                        break;
                    }

                    default:
                        _logger.LogCritical($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'");
                        return Result.Error($"unsupported {nameof(ConfigKeyActionType)} '{action.Type}'", ErrorCode.InvalidData);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not apply actions to environment {identifier}: {e}");
                return Result.Error($"could not apply actions to environment {identifier}: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public Task<Result> Connect() => Task.FromResult(Result.Success());

        /// <inheritdoc />
        public async Task<Result> CreateEnvironment(EnvironmentIdentifier identifier)
        {
            if (await _context.Environments.AnyAsync(env => env == identifier))
            {
                _logger.LogError($"environment with id {identifier} already exists");
                return Result.Error($"environment with id {identifier} already exists", ErrorCode.EnvironmentAlreadyExists);
            }

            await _context.Environments.AddAsync(new Environment
            {
                Id = Guid.NewGuid(),
                Name = identifier.Name,
                Category = identifier.Category,
                Version = 1,
                Keys = new List<EnvironmentKey>()
            });

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save new Environment {identifier} to database: {e}");
                return Result.Error($"could not save new Environment {identifier} to database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> CreateStructure(StructureIdentifier identifier)
        {
            if (await _context.Structures.AnyAsync(str => str == identifier))
            {
                _logger.LogError($"structure with id {identifier} already exists");
                return Result.Error($"structure with id {identifier} already exists", ErrorCode.StructureAlreadyExists);
            }

            await _context.Structures.AddAsync(new Structure
            {
                Id = Guid.NewGuid(),
                Name = identifier.Name,
                Version = identifier.Version,
                Keys = new List<StructureKey>()
            });

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not save new Structure {identifier} to database: {e}");
                return Result.Error($"could not save new Structure {identifier} to database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteEnvironment(EnvironmentIdentifier identifier)
        {
            var foundEnvironment = await _context.Environments.FirstOrDefaultAsync(env => env == identifier);

            if (foundEnvironment is null)
                return Result.Success();

            _context.Environments.Remove(foundEnvironment);

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete environment {identifier} from database: {e}");
                return Result.Error($"could not delete environment {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteStructure(StructureIdentifier identifier)
        {
            var foundStructure = await _context.Structures.FirstOrDefaultAsync(str => str == identifier);

            if (foundStructure is null)
                return Result.Success();

            _context.Structures.Remove(foundStructure);

            try
            {
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (DbUpdateException e)
            {
                _logger.LogError($"could not delete Structure {identifier} from database: {e}");
                return Result.Error($"could not delete Structure {identifier} from database: {e}", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context?.Dispose();
        }

        private class Context : DbContext
        {
            public DbSet<Environment> Environments { get; set; }

            public DbSet<Structure> Structures { get; set; }

            /// <inheritdoc />
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                base.OnConfiguring(optionsBuilder);

                optionsBuilder.UseSqlite("DataSource=./projected.db");
            }

            /// <inheritdoc />
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Environment>();
                modelBuilder.Entity<EnvironmentKey>();
                modelBuilder.Entity<Structure>();
                modelBuilder.Entity<StructureKey>();
            }
        }

        private class Environment : IEquatable<Environment>
        {
            public string Category { get; set; }

            public Guid Id { get; set; }

            public List<EnvironmentKey> Keys { get; set; }

            public string Name { get; set; }

            public int Version { get; set; }

            /// <inheritdoc />
            public bool Equals(Environment other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id.Equals(other.Id) &&
                       Version == other.Version &&
                       string.Equals(Category, other.Category) &&
                       string.Equals(Name, other.Name) &&
                       Equals(Keys, other.Keys);
            }

            // if either object is null => false
            // compare Category and Name for equality - Ordinal Ignoring case-sensitivity
            public static bool operator ==(Environment environment, EnvironmentIdentifier identifier)
                => !(environment is null) &&
                   !(identifier is null) &&
                   environment.Category.Equals(identifier.Category, StringComparison.OrdinalIgnoreCase) &&
                   environment.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase);

            public static bool operator !=(Environment environment, EnvironmentIdentifier identifier) => !(environment == identifier);

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Environment) obj);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id.GetHashCode();
                    hashCode = (hashCode * 397) ^ Version;
                    hashCode = (hashCode * 397) ^ (Category != null ? Category.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Keys != null ? Keys.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private class EnvironmentKey
        {
            public Guid EnvironmentId { get; set; }

            public Guid Id { get; set; }

            public string Key { get; set; }

            public string Value { get; set; }
        }

        private class Structure : IEquatable<Structure>
        {
            public Guid Id { get; set; }

            public List<StructureKey> Keys { get; set; }

            public string Name { get; set; }

            public int Version { get; set; }

            /// <inheritdoc />
            public bool Equals(Structure other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id.Equals(other.Id) &&
                       Equals(Keys, other.Keys) &&
                       string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                       Version == other.Version;
            }

            public static bool operator ==(Structure structure, StructureIdentifier identifier)
                => !(structure is null) &&
                   !(identifier is null) &&
                   structure.Version == identifier.Version &&
                   structure.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase);

            public static bool operator !=(Structure structure, StructureIdentifier identifier) => !(structure == identifier);

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Structure) obj);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Keys != null ? Keys.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Name) : 0);
                    hashCode = (hashCode * 397) ^ Version;
                    return hashCode;
                }
            }
        }

        private class StructureKey
        {
            public Guid Id { get; set; }

            public string Key { get; set; }

            public Guid StructureId { get; set; }

            public string Value { get; set; }
        }
    }
}