using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Projection.DomainEventHandlers
{
    public class EnvironmentDeletedHandler : IDomainEventHandler<EnvironmentDeleted>
    {
        private readonly IConfigurationDatabase _database;
        private readonly ILogger<EnvironmentDeletedHandler> _logger;

        /// <inheritdoc />
        public EnvironmentDeletedHandler(IConfigurationDatabase database,
                                         ILogger<EnvironmentDeletedHandler> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task HandleDomainEvent(EnvironmentDeleted domainEvent)
        {
            _logger.LogInformation($"deleting environment {domainEvent.Identifier}");

            await _database.DeleteEnvironment(domainEvent.Identifier);
        }
    }
}