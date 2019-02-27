using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentKeysImportedHandler : IDomainEventHandler<EnvironmentKeysImported>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<EnvironmentKeysModifiedHandler> _logger;

        /// <inheritdoc />
        public EnvironmentKeysImportedHandler(IConfigurationDatabase database,
                                              ILogger<EnvironmentKeysModifiedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentKeysImported domainEvent)
        {
            _logger.LogInformation($"importing '{domainEvent.ModifiedKeys.Length}' keys into '{domainEvent.Identifier}'");

            var setKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Set);
            var deletedKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Delete);

            _logger.LogInformation($"setting '{setKeys}' keys, deleting '{deletedKeys}' keys");

            await _database.ImportEnvironment(domainEvent.Identifier, domainEvent.ModifiedKeys);

            _logger.LogInformation($"generating autocomplete-data for environment '{domainEvent.Identifier}'");

            await _database.GenerateEnvironmentKeyAutocompleteData(domainEvent.Identifier);
        }
    }
}