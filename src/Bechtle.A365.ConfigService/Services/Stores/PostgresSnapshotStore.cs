using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace Bechtle.A365.ConfigService.Services.Stores
{
    /// <summary>
    ///     <see cref="ISnapshotStore"/> that saves all snapshots in the configured Postgres-DB
    /// </summary>
    public class PostgresSnapshotStore : ISnapshotStore
    {
        private readonly ILogger _logger;
        private readonly PostgresSnapshotContext _context;

        /// <inheritdoc />
        public PostgresSnapshotStore(ILogger<PostgresSnapshotStore> logger, PostgresSnapshotContext context)
        {
            _logger = logger;
            _context = context;
            _context.Database.EnsureCreated();
        }

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetEnvironmentList()
            => GetInternal(nameof(StreamedEnvironmentList));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetEnvironment(EnvironmentIdentifier identifier)
            => GetInternal(nameof(StreamedEnvironment), identifier.ToString());

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetStructureList()
            => GetInternal(nameof(StreamedStructureList));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetStructure(StructureIdentifier identifier)
            => GetInternal(nameof(StreamedStructure), identifier.ToString());

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetConfigurationList()
            => GetInternal(nameof(StreamedConfigurationList));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetConfiguration(ConfigurationIdentifier identifier)
            => GetInternal(nameof(StreamedConfiguration), identifier.ToString());

        /// <summary>
        ///     get the first snapshot that first the given <paramref name="name"/>. used when both DataType and Identifier have the same value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Task<IResult<StreamedObjectSnapshot>> GetInternal(string name)
            => GetInternal(s => s.DataType == name && s.Identifier == name);

        /// <summary>
        ///     get the first snapshot that fits the given <paramref name="dataType"/> and <paramref name="identifier"/>
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private Task<IResult<StreamedObjectSnapshot>> GetInternal(string dataType, string identifier)
            => GetInternal(s => s.DataType == dataType && s.Identifier == identifier);

        /// <summary>
        ///     filter all snapshots based on the given <paramref name="filter"/>, and convert the first one to <see cref="StreamedObjectSnapshot"/>
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private async Task<IResult<StreamedObjectSnapshot>> GetInternal(Expression<Func<PostgresSnapshot, bool>> filter)
        {
            try
            {
                var result = await _context.Snapshots
                                           .Where(filter)
                                           .FirstOrDefaultAsync();

                if (result is null)
                    return Result.Error<StreamedObjectSnapshot>("could not retrieve snapshot from Postgres", ErrorCode.DbQueryError);

                return Result.Success(new StreamedObjectSnapshot
                {
                    Identifier = result.Identifier,
                    Version = result.Version,
                    DataType = result.DataType,
                    JsonData = result.JsonData
                });
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve snapshot from Postgres");
                return Result.Error<StreamedObjectSnapshot>("could not retrieve snapshot from Postgres", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> SaveSnapshots(IList<StreamedObjectSnapshot> snapshots)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldSnapshots = (await _context.Snapshots.ToListAsync())
                                   .Where(dbSnapshot => snapshots.Any(
                                              newSnapshot => dbSnapshot.DataType == newSnapshot.DataType
                                                             && dbSnapshot.Identifier == newSnapshot.Identifier))
                                   .ToList();

                _context.Snapshots.RemoveRange(oldSnapshots);

                _context.SaveChanges();

                _context.Snapshots.AddRange(snapshots.Select(s => new PostgresSnapshot
                {
                    Identifier = s.Identifier,
                    Version = s.Version,
                    DataType = s.DataType,
                    JsonData = s.JsonData
                }));

                _context.SaveChanges();

                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not save Snapshots in DB");
                await transaction.RollbackAsync();

                return Result.Error("could not save Snapshots in Postgres", ErrorCode.DbUpdateError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<long>> GetLatestSnapshotNumbers()
        {
            try
            {
                return Result.Success(await _context.Snapshots.MaxAsync(s => s.Version));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "could not retrieve highest snapshot-number from Postgres");
                return Result.Error<long>("could not retrieve highest snapshot-number from Postgres", ErrorCode.DbQueryError);
            }
        }

        /// <summary>
        ///     DbContext for <see cref="PostgresSnapshotStore"/>
        /// </summary>
        public class PostgresSnapshotContext : DbContext
        {
            /// <inheritdoc />
            public PostgresSnapshotContext(DbContextOptions options) : base(options)
            {
            }

            internal DbSet<PostgresSnapshot> Snapshots { get; set; }
        }

        internal class PostgresSnapshot
        {
            [Key]
            public Guid Id { get; set; }

            public string DataType { get; set; }

            public string Identifier { get; set; }

            [Column(TypeName = "jsonb")]
            public string JsonData { get; set; }

            public long Version { get; set; }
        }
    }
}