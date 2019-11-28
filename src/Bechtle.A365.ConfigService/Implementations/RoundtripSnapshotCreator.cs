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

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     implementation of <see cref="ISnapshotCreator" /> that will always do a complete roundtrip through all events in a given <see cref="IEventStore" />
    /// </summary>
    public class RoundtripSnapshotCreator : ISnapshotCreator
    {
        private readonly IConfigurationCompiler _compiler;
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IEventStore _eventStore;
        private readonly IConfigurationParser _parser;
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
            // take all objects and group all Configs by their Target-Environment
            var envGroups = domainObjects.OfType<PreparedConfiguration>()
                                         .GroupBy(c => c.Identifier.Environment);

            await Task.WhenAll(envGroups.AsParallel().Select(async group =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // use only the last version of last version of each structure
                var selectedConfigs = group.GroupBy(c => c.Identifier.Structure.Name)
                                           .Select(g => g.OrderBy(c => c.Identifier.Structure.Version).First())
                                           .OrderBy(c => c.CurrentVersion)
                                           .ToList();

                foreach (var config in selectedConfigs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await config.Compile(_domainObjectStore, _compiler, _parser, _translator);
                }
            }));

            return domainObjects.Select(o => o.CreateSnapshot()).ToList();
        }

        private bool StreamProcessor((StoredEvent, DomainEvent) tuple, IList<DomainObject> domainObjects)
        {
            var (recordedEvent, domainEvent) = tuple;

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

            var replayedEvent = new ReplayedEvent
            {
                DomainEvent = domainEvent,
                UtcTime = recordedEvent.UtcTime,
                Version = recordedEvent.EventNumber
            };

            // apply every event to every DomainObject - they should dismiss them if the event doesn't fit
            domainObjects.AsParallel()
                         .ForAll(domainObject => domainObject.ApplyEvent(replayedEvent));

            // return true to continue StreamProcessing
            return true;
        }
    }
}