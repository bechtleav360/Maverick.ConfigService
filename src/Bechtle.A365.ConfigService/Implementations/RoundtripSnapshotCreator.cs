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
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IJsonTranslator _translator;

        /// <inheritdoc />
        public RoundtripSnapshotCreator(IEventStore eventStore,
                                        IConfigurationParser parser,
                                        IConfigurationCompiler compiler,
                                        IJsonTranslator translator,
                                        IDomainObjectStore domainObjectStore)
        {
            _eventStore = eventStore;
            _parser = parser;
            _compiler = compiler;
            _translator = translator;
            _domainObjectStore = domainObjectStore;
        }

        /// <inheritdoc />
        public async Task<IList<DomainObjectSnapshot>> CreateAllSnapshots(CancellationToken cancellationToken)
        {
            var domainObjects = new List<DomainObject>
            {
                new ConfigEnvironmentList(),
                new ConfigStructureList(),
                new PreparedConfigurationList()
            };

            await _eventStore.ReplayEventsAsStream(tuple => StreamProcessor(tuple, domainObjects));

            return await CreateSnapshotsInternal(domainObjects, cancellationToken);
        }

        private async Task<IList<DomainObjectSnapshot>> CreateSnapshotsInternal(IList<DomainObject> domainObjects, CancellationToken cancellationToken)
        {
            foreach (var config in domainObjects.OfType<PreparedConfiguration>())
            {
                if (cancellationToken.IsCancellationRequested)
                    return new List<DomainObjectSnapshot>();

                await config.Compile(_domainObjectStore, _compiler, _parser, _translator);
            }

            return domainObjects.Select(o => o.CreateSnapshot()).ToList();
        }

        private bool StreamProcessor((RecordedEvent, DomainEvent) tuple, IList<DomainObject> domainObjects)
        {
            var (recordedEvent, domainEvent) = tuple;

            var replayedEvent = new ReplayedEvent
            {
                DomainEvent = domainEvent,
                UtcTime = recordedEvent.Created.ToUniversalTime(),
                Version = recordedEvent.EventNumber
            };

            // create or delete StreamedObjects as necessary
            switch (domainEvent)
            {
                case ConfigurationBuilt built:
                    if (domainObjects.OfType<PreparedConfiguration>().FirstOrDefault(o => o.Identifier == built.Identifier) is null)
                        domainObjects.Add(new PreparedConfiguration(built.Identifier));
                    break;

                case DefaultEnvironmentCreated created:
                    if (domainObjects.OfType<ConfigEnvironment>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        domainObjects.Add(new ConfigEnvironment(created.Identifier));
                    break;

                case EnvironmentCreated created:
                    if (domainObjects.OfType<ConfigEnvironment>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        domainObjects.Add(new ConfigEnvironment(created.Identifier));
                    break;

                case EnvironmentDeleted deleted:
                    domainObjects.RemoveRange(domainObjects.Where(o => o is ConfigEnvironment e && e.Identifier == deleted.Identifier));
                    break;

                case StructureCreated created:
                    if (domainObjects.OfType<ConfigStructure>().FirstOrDefault(o => o.Identifier == created.Identifier) is null)
                        domainObjects.Add(new ConfigStructure(created.Identifier));
                    break;

                case StructureDeleted deleted:
                    domainObjects.RemoveRange(domainObjects.Where(o => o is ConfigStructure e && e.Identifier == deleted.Identifier));
                    break;
            }

            // apply every event to every DomainObject - they should dismiss them if the event doesn't fit
            foreach (var domainObject in domainObjects)
                domainObject.ApplyEvent(replayedEvent);

            // return true to continue StreamProcessing
            return true;
        }
    }
}