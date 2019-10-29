using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     default ObjectStore using <see cref="IEventStore"/> and <see cref="ISnapshotStore"/> for retrieving Objects
    /// </summary>
    public class StreamedObjectStore : IStreamedStore
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;

        /// <inheritdoc />
        public StreamedObjectStore(IEventStore eventStore,
                                   ISnapshotStore snapshotStore)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
        }

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier)
            => GetEnvironment(identifier, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier, long maxVersion)
        {
            var environment = new StreamedEnvironment(identifier);

            var latestSnapshot = await _snapshotStore.GetEnvironment(identifier);

            if (!latestSnapshot.IsError)
                environment.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(environment, maxVersion);

            return Result.Success(environment);
        }

        /// <inheritdoc />
        public Task<IResult<StreamedEnvironmentList>> GetEnvironmentList()
            => GetEnvironmentList(long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedEnvironmentList>> GetEnvironmentList(long maxVersion)
        {
            var list = new StreamedEnvironmentList();

            var latestSnapshot = await _snapshotStore.GetEnvironmentList();

            if (!latestSnapshot.IsError)
                list.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(list, maxVersion);

            return Result.Success(list);
        }

        /// <inheritdoc />
        public Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier)
            => GetStructure(identifier, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier, long maxVersion)
        {
            var structure = new StreamedStructure(identifier);

            var latestSnapshot = await _snapshotStore.GetStructure(identifier);

            if (!latestSnapshot.IsError)
                structure.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(structure, maxVersion);

            return Result.Success(structure);
        }

        /// <inheritdoc />
        public Task<IResult<StreamedStructureList>> GetStructureList()
            => GetStructureList(long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedStructureList>> GetStructureList(long maxVersion)
        {
            var list = new StreamedStructureList();

            var latestSnapshot = await _snapshotStore.GetStructureList();

            if (!latestSnapshot.IsError)
                list.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(list, maxVersion);

            return Result.Success(list);
        }

        /// <inheritdoc />
        public Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier)
            => GetConfiguration(identifier, long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedConfiguration>> GetConfiguration(ConfigurationIdentifier identifier, long maxVersion)
        {
            var configuration = new StreamedConfiguration(identifier);

            var latestSnapshot = await _snapshotStore.GetConfiguration(identifier);

            if (!latestSnapshot.IsError)
                configuration.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(configuration, maxVersion);

            return Result.Success(configuration);
        }

        /// <inheritdoc />
        public Task<IResult<StreamedConfigurationList>> GetConfigurationList()
            => GetConfigurationList(long.MaxValue);

        /// <inheritdoc />
        public async Task<IResult<StreamedConfigurationList>> GetConfigurationList(long maxVersion)
        {
            var list = new StreamedConfigurationList();

            var latestSnapshot = await _snapshotStore.GetConfigurationList();

            if (!latestSnapshot.IsError)
                list.ApplySnapshot(latestSnapshot.Data);

            await StreamObjectToVersion(list, maxVersion);

            return Result.Success(list);
        }

        private async Task StreamObjectToVersion(StreamedObject streamedObject, long maxVersion)
        {
            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                // stop at the designated max-version
                if (recordedEvent.EventNumber > maxVersion)
                    return false;

                streamedObject.ApplyEvent(new StreamedEvent
                {
                    UtcTime = recordedEvent.Created.ToUniversalTime(),
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            }, startIndex: streamedObject.CurrentVersion);
        }
    }
}