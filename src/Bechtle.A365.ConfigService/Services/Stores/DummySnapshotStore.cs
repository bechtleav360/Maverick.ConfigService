﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Services.Stores
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

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetStructureList()
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetStructure(StructureIdentifier identifier)
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetConfigurationList()
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetConfiguration(ConfigurationIdentifier identifier)
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetSnapshot<T>(string identifier) where T : StreamedObject
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<StreamedObjectSnapshot>> GetSnapshot(string dataType, string identifier)
            => Task.FromResult(Result.Error<StreamedObjectSnapshot>(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult> SaveSnapshots(IList<StreamedObjectSnapshot> snapshots)
            => Task.FromResult(Result.Error(string.Empty, ErrorCode.Undefined));

        /// <inheritdoc />
        public Task<IResult<long>> GetLatestSnapshotNumbers()
            => Task.FromResult(Result.Error<long>(string.Empty, ErrorCode.Undefined));
    }
}