using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.Common.Utilities.Extensions;
using Bechtle.A365.ConfigService.Common.Compilation;
using Bechtle.A365.ConfigService.Common.Converters;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Parsing;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     implementation of <see cref="ISnapshotCreator" /> that will always do a complete roundtrip through all events in a given <see cref="IEventStore" />
    /// </summary>
    public class RoundtripSnapshotCreator : ISnapshotCreator
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IEventStore _eventStore;
        private readonly IConfigurationParser _parser;
        private readonly IStreamedStore _streamedStore;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public RoundtripSnapshotCreator(IEventStore eventStore,
                                        IConfigurationParser parser,
                                        IConfigurationCompiler compiler,
                                        IJsonTranslator translator,
                                        IStreamedStore streamedStore)
        {
            _eventStore = eventStore;
            _parser = parser;
            _compiler = compiler;
            _translator = translator;
            _streamedStore = streamedStore;
        }

        /// <inheritdoc />
        public async Task<IList<DomainObjectSnapshot>> CreateAllSnapshots(CancellationToken cancellationToken)
        {
            var streamedObjects = new List<DomainObject>();

            await _eventStore.ReplayEventsAsStream(tuple => StreamProcessor(tuple, streamedObjects));

            return await CreateSnapshotsInternal(streamedObjects, cancellationToken);
        }

        private async Task<IList<DomainObjectSnapshot>> CreateSnapshotsInternal(IList<DomainObject> streamedObjects, CancellationToken cancellationToken)
        {
            foreach (var config in streamedObjects.OfType<StreamedConfiguration>())
            {
                if (cancellationToken.IsCancellationRequested)
                    return new List<DomainObjectSnapshot>();

                await config.Compile(_streamedStore, _compiler, _parser, _translator);
            }

            return streamedObjects.Select(o => o.CreateSnapshot()).ToList();
        }

        private bool StreamProcessor((RecordedEvent, DomainEvent) tuple, IList<DomainObject> streamedObjects)
        {
            var (recordedEvent, domainEvent) = tuple;

            var streamedEvent = new StreamedEvent
            {
                DomainEvent = domainEvent,
                UtcTime = recordedEvent.Created.ToUniversalTime(),
                Version = recordedEvent.EventNumber
            };

            // create or delete StreamedObjects as necessary
            switch (domainEvent)
            {
                case ConfigurationBuilt built:
                    if (streamedObjects.OfType<StreamedConfiguration>().FirstOrDefault(o => o.Identifier == built.Identifier) is null)
                        streamedObjects.Add(new StreamedConfiguration(built.Identifier));
                    break;

                case DefaultEnvironmentCreated created:
                    if (streamedObjects.OfType<ConfigEnvironment>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        streamedObjects.Add(new ConfigEnvironment(created.Identifier));
                    break;

                case EnvironmentCreated created:
                    if (streamedObjects.OfType<ConfigEnvironment>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        streamedObjects.Add(new ConfigEnvironment(created.Identifier));
                    break;

                case EnvironmentDeleted deleted:
                    streamedObjects.RemoveRange(streamedObjects.Where(o => o is ConfigEnvironment e && e.Identifier == deleted.Identifier));
                    break;

                case StructureCreated created:
                    if (streamedObjects.OfType<ConfigStructure>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        streamedObjects.Add(new ConfigStructure(created.Identifier));
                    break;

                case StructureDeleted deleted:
                    streamedObjects.RemoveRange(streamedObjects.Where(o => o is ConfigStructure e && e.Identifier == deleted.Identifier));
                    break;
            }

            // apply every event to every DomainObject - they should dismiss them if the event doesn't fit
            foreach (var streamedObject in streamedObjects)
                streamedObject.ApplyEvent(streamedEvent);

            // return true to continue StreamProcessing
            return true;
        }
    }
}