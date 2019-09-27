using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureDeletedHandler : IDomainEventHandler<StructureDeleted>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<StructureDeletedHandler> _logger;

        /// <inheritdoc />
        public StructureDeletedHandler(IConfigurationDatabase database,
                                       ILogger<StructureDeletedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureDeleted domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent), $"{nameof(domainEvent)} must not be null");

            _logger.LogInformation($"deleting structure {domainEvent.Identifier}");

            await _database.DeleteStructure(domainEvent.Identifier);
        }
    }
}