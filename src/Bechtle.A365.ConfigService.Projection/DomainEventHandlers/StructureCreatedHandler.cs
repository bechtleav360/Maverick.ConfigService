using System;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class StructureCreatedHandler : IDomainEventHandler<StructureCreated>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<StructureCreatedHandler> _logger;

        /// <inheritdoc />
        public StructureCreatedHandler(IConfigurationDatabase database,
                                       ILogger<StructureCreatedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(StructureCreated domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent), $"{nameof(domainEvent)} must not be null");

            _logger.LogInformation($"creating structure for {domainEvent.Identifier} " +
                                   $"with '{domainEvent.Keys.Count}' keys " +
                                   $"and '{domainEvent.Variables.Count}' variables");

            await _database.CreateStructure(domainEvent.Identifier,
                                            domainEvent.Keys,
                                            domainEvent.Variables);
        }
    }
}