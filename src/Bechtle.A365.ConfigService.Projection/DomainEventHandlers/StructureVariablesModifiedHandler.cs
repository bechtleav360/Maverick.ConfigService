using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureVariablesModifiedHandler : IDomainEventHandler<StructureVariablesModified>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<StructureVariablesModifiedHandler> _logger;

        /// <inheritdoc />
        public StructureVariablesModifiedHandler(IConfigurationDatabase database,
                                                 ILogger<StructureVariablesModifiedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureVariablesModified domainEvent)
        {
            var setKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Set);
            var deletedKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Delete);

            _logger.LogInformation($"setting '{setKeys}' keys, deleting '{deletedKeys}' keys");

            await _database.ApplyChanges(domainEvent.Identifier,
                                         domainEvent.ModifiedKeys);
        }
    }
}