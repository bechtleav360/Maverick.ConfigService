using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     SnapshotStore that stores all its snapshots in memory
    /// </summary>
    public sealed class MemorySnapshotStore : ISnapshotStore
    {
        private static readonly ConcurrentDictionary<string, AnnotatedSnapshot> Snapshots
            = new ConcurrentDictionary<string, AnnotatedSnapshot>(StringComparer.Ordinal);

        private readonly ILogger<MemorySnapshotStore> _logger;

        /// <inheritdoc />
        public MemorySnapshotStore(ILogger<MemorySnapshotStore> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

        /// <inheritdoc />
        public void Dispose()
        {
            // nothing to dispose of
        }

        /// <inheritdoc />
        public Task<IResult<long>> GetLatestSnapshotNumbers()
        {
            _logger.LogDebug("getting latest snapshot-version");

            var result = Snapshots.Any()
                             ? Snapshots.Select(pair => pair.Value.MetaVersion).Min()
                             : long.MinValue;

            _logger.LogDebug($"currently latest snapshot: '{result}'");

            return Task.FromResult(Result.Success(result));
        }

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier) where T : DomainObject
            => GetSnapshot<T>(identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier, long maxVersion) where T : DomainObject
            => GetSnapshot(typeof(T).Name, identifier, maxVersion);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier)
            => GetSnapshot(dataType, identifier, long.MaxValue);

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier, long maxVersion)
        {
            _logger.LogDebug($"retrieving snapshot for '{dataType}' / '{identifier}' below or at version '{maxVersion}'");

            var results = Snapshots.Select(pair => pair.Value.Snapshot)
                                   .Where(s => s.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase)
                                               && s.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)
                                               && s.Version <= maxVersion)
                                   .ToList();


            if (results.Any())
            {
                _logger.LogDebug($"found '{results.Count}' possible snapshots, choosing latest one");

                var latestSnapshot = results.OrderByDescending(s => s.Version)
                                            .First();

                _logger.LogDebug($"latest snapshot for '{dataType}' / '{identifier}' below or at version '{maxVersion}' " +
                                 $"= {latestSnapshot.Version} / {latestSnapshot.MetaVersion}");

                return Task.FromResult(Result.Success(latestSnapshot));
            }

            return Task.FromResult(
                Result.Error<DomainObjectSnapshot>(
                    $"no snapshot found for DataType: '{dataType}', Identifier: '{identifier}', Version: {maxVersion}",
                    ErrorCode.NotFound));
        }

        /// <inheritdoc />
        public Task<IResult> SaveSnapshots(IList<DomainObjectSnapshot> snapshots)
        {
            if (snapshots is null || !snapshots.Any())
                return Task.FromResult(Result.Success());

            foreach (var snapshot in snapshots)
            {
                _logger.LogDebug($"saving snapshot for '{snapshot.Identifier}'");
                Snapshots[MakeKeyFor(snapshot)] = new AnnotatedSnapshot
                {
                    MetaVersion = snapshot.MetaVersion,
                    Snapshot = snapshot
                };
            }

            return Task.FromResult(Result.Success());
        }

        private static string MakeKeyFor(DomainObjectSnapshot snapshot) => MakeKeyFor(snapshot.DataType, snapshot.Identifier, snapshot.Version);

        private static string MakeKeyFor(string dataType, string identifier, long version)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{dataType};{identifier};{version};"));

        private struct AnnotatedSnapshot
        {
            public DomainObjectSnapshot Snapshot { get; set; }

            public long MetaVersion { get; set; }
        }
    }
}