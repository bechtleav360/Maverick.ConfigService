using System;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentKeysModifiedHandler : IDomainEventHandler<EnvironmentKeysModified>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<EnvironmentKeysModifiedHandler> _logger;

        /// <inheritdoc />
        public EnvironmentKeysModifiedHandler(IConfigurationDatabase database,
                                              ILogger<EnvironmentKeysModifiedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentKeysModified domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent), $"{nameof(domainEvent)} must not be null");

            var setKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Set);
            var deletedKeys = domainEvent.ModifiedKeys.Count(k => k.Type == ConfigKeyActionType.Delete);

            _logger.LogInformation($"setting '{setKeys}' keys, deleting '{deletedKeys}' keys");

            var applyChangesResult = await _database.ApplyChanges(domainEvent.Identifier,
                                                                  domainEvent.ModifiedKeys);

            if (applyChangesResult.IsError)
                throw new Exception($"failed to apply changes to '{domainEvent.Identifier}': {applyChangesResult.Message}");

            _logger.LogInformation($"generating autocomplete-data for environment '{domainEvent.Identifier}'");

            var autoCompleteResult = await _database.GenerateEnvironmentKeyAutocompleteData(domainEvent.Identifier);

            if (autoCompleteResult.IsError)
                throw new Exception($"failed to generate autocomplete-data after applying changes to environment: '{domainEvent.Identifier}'");
        }
    }
}