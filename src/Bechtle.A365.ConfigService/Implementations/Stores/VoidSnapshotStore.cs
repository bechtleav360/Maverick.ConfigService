using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     SnapshotStore that doesn't actually store anything
    /// </summary>
    public sealed class VoidSnapshotStore : ISnapshotStore
    {
        /// <inheritdoc />
        public void Dispose()
        {
            // nothing to dispose of
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

        /// <inheritdoc />
        public Task<IResult<long>> GetLatestSnapshotNumbers() => Task.FromResult(Result.Success(0L));

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier) where T : DomainObject
            => Task.FromResult(Result.Error<DomainObjectSnapshot>("not supported", ErrorCode.NotFound));

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot<T>(string identifier, long maxVersion) where T : DomainObject
            => Task.FromResult(Result.Error<DomainObjectSnapshot>("not supported", ErrorCode.NotFound));

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier)
            => Task.FromResult(Result.Error<DomainObjectSnapshot>("not supported", ErrorCode.NotFound));

        /// <inheritdoc />
        public Task<IResult<DomainObjectSnapshot>> GetSnapshot(string dataType, string identifier, long maxVersion)
            => Task.FromResult(Result.Error<DomainObjectSnapshot>("not supported", ErrorCode.NotFound));

        /// <inheritdoc />
        public Task<IResult> SaveSnapshots(IList<DomainObjectSnapshot> snapshots)
            => Task.FromResult(Result.Error("not supported", ErrorCode.NotFound));
    }
}