using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Services;
using Bechtle.A365.ConfigService.Services.Stores;

namespace Bechtle.A365.ConfigService.DomainObjects
{
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
        public async Task<IResult<StreamedEnvironment>> GetEnvironment(EnvironmentIdentifier identifier)
        {
            var environment = new StreamedEnvironment(identifier);

            var latestSnapshot = await _snapshotStore.GetEnvironment(identifier);

            if (!latestSnapshot.IsError)
                environment.ApplySnapshot(latestSnapshot.Data);

            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                environment.ApplyEvent(new StreamedEvent
                {
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            });

            return Result.Success(environment);
        }

        /// <inheritdoc />
        public async Task<IResult<StreamedStructure>> GetStructure(StructureIdentifier identifier)
        {
            var structure = new StreamedStructure(identifier);

            var latestSnapshot = await _snapshotStore.GetStructure(identifier);

            if (!latestSnapshot.IsError)
                structure.ApplySnapshot(latestSnapshot.Data);

            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                structure.ApplyEvent(new StreamedEvent
                {
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            });

            return Result.Success(structure);
        }

        /// <inheritdoc />
        public async Task<IResult<StreamedStructureList>> GetStructureList()
        {
            var list = new StreamedStructureList();

            var latestSnapshot = await _snapshotStore.GetStructureList();

            if (!latestSnapshot.IsError)
                list.ApplySnapshot(latestSnapshot.Data);

            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                list.ApplyEvent(new StreamedEvent
                {
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            });

            return Result.Success(list);
        }

        /// <inheritdoc />
        public async Task<IResult<StreamedEnvironmentList>> GetEnvironmentList()
        {
            var list = new StreamedEnvironmentList();

            var latestSnapshot = await _snapshotStore.GetEnvironmentList();

            if (!latestSnapshot.IsError)
                list.ApplySnapshot(latestSnapshot.Data);

            // @TODO: start streaming from latestSnapshot.Version
            await _eventStore.ReplayEventsAsStream(tuple =>
            {
                var (recordedEvent, domainEvent) = tuple;

                list.ApplyEvent(new StreamedEvent
                {
                    Version = recordedEvent.EventNumber,
                    DomainEvent = domainEvent
                });

                return true;
            });

            return Result.Success(list);
        }
    }
}