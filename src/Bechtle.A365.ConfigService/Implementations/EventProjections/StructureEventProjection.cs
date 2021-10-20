using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ServiceBase.EventStore.Abstractions;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.EventProjections
{
    /// <summary>
    ///     Projection for all DomainEvents regarding <see cref="ConfigStructure" />
    /// </summary>
    public class StructureEventProjection :
        EventProjectionBase,
        IDomainEventProjection<StructureCreated>,
        IDomainEventProjection<StructureDeleted>,
        IDomainEventProjection<StructureVariablesModified>
    {
        private readonly ILogger<StructureEventProjection> _logger;
        private readonly IDomainObjectStore _objectStore;

        /// <summary>
        ///     Create a new instance of <see cref="StructureEventProjection"/>
        /// </summary>
        /// <param name="objectStore">storage for generated configs</param>
        /// <param name="logger">logger to write diagnostic information</param>
        public StructureEventProjection(
            IDomainObjectStore objectStore,
            ILogger<StructureEventProjection> logger)
            : base(objectStore)
        {
            _objectStore = objectStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<StructureCreated> domainEvent)
        {
            var structure = new ConfigStructure(domainEvent.Payload.Identifier)
            {
                Keys = domainEvent.Payload.Keys,
                Variables = domainEvent.Payload.Variables,
                CurrentVersion = (long)eventHeader.EventNumber,
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                CreatedAt = domainEvent.Timestamp.ToUniversalTime()
            };

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(structure);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<StructureDeleted> domainEvent)
        {
            await _objectStore.Remove<ConfigStructure, StructureIdentifier>(domainEvent.Payload.Identifier);
        }

        /// <inheritdoc />
        public async Task ProjectChanges(
            StreamedEventHeader eventHeader,
            IDomainEvent<StructureVariablesModified> domainEvent)
        {
            IResult<ConfigStructure> structureResult = await _objectStore.Load<ConfigStructure, StructureIdentifier>(domainEvent.Payload.Identifier);

            if (structureResult.IsError)
            {
                _logger.LogWarning(
                    "event received to modify structure-vars, but structure wasn't found in configured store: {ErrorCode} {Message}",
                    structureResult.Code,
                    structureResult.Message);
                return;
            }

            ConfigStructure structure = structureResult.CheckedData;
            Dictionary<string, string?> modifiedVariables = structure.Variables;

            foreach (ConfigKeyAction deletion in domainEvent.Payload
                                                            .ModifiedKeys
                                                            .Where(action => action.Type == ConfigKeyActionType.Delete))
            {
                if (modifiedVariables.ContainsKey(deletion.Key))
                {
                    modifiedVariables.Remove(deletion.Key);
                }
            }

            foreach (ConfigKeyAction change in domainEvent.Payload
                                                          .ModifiedKeys
                                                          .Where(action => action.Type == ConfigKeyActionType.Set))
            {
                modifiedVariables[change.Key] = change.Value;
            }

            ConfigStructure modifiedStructure = new(structure)
            {
                ChangedAt = domainEvent.Timestamp.ToUniversalTime(),
                ChangedBy = "Anonymous",
                CurrentVersion = (long)eventHeader.EventNumber,
                Variables = modifiedVariables
            };

            await _objectStore.Store<ConfigStructure, StructureIdentifier>(modifiedStructure);
        }
    }
}
