using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services
{
    /// <inheritdoc />
    public class DummySnapshotStore : ISnapshotStore
    {
        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetEnvironmentList()
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetEnvironment(EnvironmentIdentifier identifier)
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));
    }
}